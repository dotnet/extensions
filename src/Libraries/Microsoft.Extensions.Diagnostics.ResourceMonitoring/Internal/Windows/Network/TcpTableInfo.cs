// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;

internal sealed class TcpTableInfo
{
    private readonly object _lock = new();
    private readonly FrozenSet<uint> _localIPAddresses;
    private readonly TimeSpan _samplingInterval;
    internal delegate uint GetTcpTableDelegate(IntPtr pTcpTable, ref uint pdwSize, bool bOrder);
    private static GetTcpTableDelegate _getTcpTable = SafeNativeMethods.GetTcpTable;
    private static TimeProvider TimeProvider => TimeProvider.System;
    private TcpStateInfo _snapshot = new();
    private DateTimeOffset _refreshAfter;

    public TcpTableInfo(IOptions<ResourceMonitoringOptions> options)
    {
        var stringAddresses = options.Value.SourceIpAddresses;
        _localIPAddresses = stringAddresses
#pragma warning disable CS0618
            .Select(s => (uint)IPAddress.Parse(s).Address)
#pragma warning restore CS0618
            .ToFrozenSet();
        _samplingInterval = options.Value.SamplingInterval;
        _refreshAfter = default;
    }

    public TcpStateInfo GetCachingSnapshot()
    {
        lock (_lock)
        {
            var utcNow = TimeProvider.GetUtcNow();
            if (_refreshAfter < utcNow)
            {
                _snapshot = GetSnapshot();
                _refreshAfter = utcNow.Add(_samplingInterval);
            }
        }

        return _snapshot;
    }

    internal static void SetGetTcpTableDelegate(GetTcpTableDelegate getTcpTableDelegate) => _getTcpTable = getTcpTableDelegate;

    private static IntPtr RetryCalling()
    {
        const int LoopTimes = 5;
        uint size = 0;
        var result = (uint)NTSTATUS.UnsuccessfulStatus;
        var tcpTable = IntPtr.Zero;

        // Stryker disable once all : Loop for 5 times as a default setting.
        for (int i = 0; i < LoopTimes; ++i)
        {
            // Stryker disable once all : True or false will not affect the results.
            result = _getTcpTable(tcpTable, ref size, false);
            switch ((NTSTATUS)result)
            {
                case NTSTATUS.Success:
                    return tcpTable;
                case NTSTATUS.InsufficientBuffer:
                    Marshal.FreeHGlobal(tcpTable);
                    tcpTable = Marshal.AllocHGlobal((int)size);
                    break;
                case NTSTATUS.UnsuccessfulStatus:
                    break;
                default:
                    Marshal.FreeHGlobal(tcpTable);
                    throw new InvalidOperationException(
                            $"Failed to GetTcpTable. Return value is {result}. " +
                            $"For more information see https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-gettcptable");
            }
        }

        Marshal.FreeHGlobal(tcpTable);
        throw new InvalidOperationException(
                $"Failed to GetTcpTable. Return value is {result}. " +
                $"For more information see https://learn.microsoft.com/en-us/windows/win32/api/iphlpapi/nf-iphlpapi-gettcptable");
    }

    private unsafe TcpStateInfo GetSnapshot()
    {
        var tcpTable = RetryCalling();
        var rawTcpTable = Marshal.PtrToStructure<MIB_TCPTABLE>(tcpTable);

        var tcpStateInfo = new TcpStateInfo();
        var offset = Marshal.OffsetOf<MIB_TCPTABLE>(nameof(MIB_TCPTABLE.Table)).ToInt32();
        var rowPtr = IntPtr.Add(tcpTable, offset);
        for (int i = 0; i < rawTcpTable.NumberOfEntries; ++i)
        {
            var row = Marshal.PtrToStructure<MIB_TCPROW>(rowPtr);
            rowPtr = IntPtr.Add(rowPtr, sizeof(MIB_TCPROW));

            if (_localIPAddresses.Count > 0 && !_localIPAddresses.Contains(row.LocalAddr))
            {
                continue;
            }

            switch (row.State)
            {
                case MIB_TCP_STATE.CLOSED:
                    tcpStateInfo.ClosedCount++;
                    break;
                case MIB_TCP_STATE.LISTEN:
                    tcpStateInfo.ListenCount++;
                    break;
                case MIB_TCP_STATE.SYN_SENT:
                    tcpStateInfo.SynSentCount++;
                    break;
                case MIB_TCP_STATE.SYN_RCVD:
                    tcpStateInfo.SynRcvdCount++;
                    break;
                case MIB_TCP_STATE.ESTAB:
                    tcpStateInfo.EstabCount++;
                    break;
                case MIB_TCP_STATE.FIN_WAIT1:
                    tcpStateInfo.FinWait1Count++;
                    break;
                case MIB_TCP_STATE.FIN_WAIT2:
                    tcpStateInfo.FinWait2Count++;
                    break;
                case MIB_TCP_STATE.CLOSE_WAIT:
                    tcpStateInfo.CloseWaitCount++;
                    break;
                case MIB_TCP_STATE.CLOSING:
                    tcpStateInfo.ClosingCount++;
                    break;
                case MIB_TCP_STATE.LAST_ACK:
                    tcpStateInfo.LastAckCount++;
                    break;
                case MIB_TCP_STATE.TIME_WAIT:
                    tcpStateInfo.TimeWaitCount++;
                    break;
                case MIB_TCP_STATE.DELETE_TCB:
                    tcpStateInfo.DeleteTcbCount++;
                    break;
            }
        }

        Marshal.FreeHGlobal(tcpTable);
        return tcpStateInfo;
    }

    private static class SafeNativeMethods
    {
        /// <summary>
        /// Import of GetTcpTable win32 function.
        /// </summary>
        /// <param name="pTcpTable">A pointer of a MIB_TCPTABLE struct.</param>
        /// <param name="pdwSize">On input, specifies the size in bytes of the buffer pointed to by the pTcpTable parameter.
        /// On output, if the buffer is not large enough to hold the returned connection table, the function sets this parameter equal to the required buffer size in bytes.</param>
        /// <param name="bOrder">A Boolean value that specifies whether the TCP connection table should be sorted.</param>
        [DllImport("Iphlpapi.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern uint GetTcpTable(IntPtr pTcpTable, ref uint pdwSize, bool bOrder);
    }
}

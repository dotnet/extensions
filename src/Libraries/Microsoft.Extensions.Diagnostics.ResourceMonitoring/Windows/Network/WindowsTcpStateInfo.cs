// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Windows.Network;

internal sealed class WindowsTcpStateInfo : ITcpStateInfoProvider
{
    private readonly object _lock = new();
    private readonly FrozenSet<uint> _localIPAddresses;
    private readonly FrozenSet<byte[]> _iPv6localIPAddresses;
    private readonly TimeSpan _samplingInterval;
    internal delegate uint GetTcpTableDelegate(IntPtr pTcpTable, ref uint pdwSize, bool bOrder);
    private GetTcpTableDelegate _getTcpTable = SafeNativeMethods.GetTcpTable;
    private GetTcpTableDelegate _getTcp6Table = SafeNativeMethods.GetTcp6Table;
    private TcpStateInfo _iPv4Snapshot = new();
    private TcpStateInfo _iPv6Snapshot = new();
    private static TimeProvider TimeProvider => TimeProvider.System;
    private DateTimeOffset _refreshAfter;

    public WindowsTcpStateInfo(IOptions<ResourceMonitoringOptions> options)
    {
        var stringAddresses = options.Value.SourceIpAddresses;
        _localIPAddresses = stringAddresses
            .Where(ip => IPAddress.TryParse(ip, out var ipAddress) && ipAddress.AddressFamily == AddressFamily.InterNetwork)
#pragma warning disable CS0618
            .Select(s => (uint)IPAddress.Parse(s).Address)
#pragma warning restore CS0618
            .ToFrozenSet();
        _iPv6localIPAddresses = stringAddresses
            .Where(ip => IPAddress.TryParse(ip, out var ipAddress) && ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
            .Select(s => IPAddress.Parse(s).GetAddressBytes())
            .ToFrozenSet(new ByteArrayEqualityComparer());
        _samplingInterval = options.Value.SamplingInterval;
        _refreshAfter = default;
    }

    public TcpStateInfo GetIpV4TcpStateInfo()
    {
        RefreshSnapshotIfNeeded();
        return _iPv4Snapshot;
    }

    public TcpStateInfo GetIpV6TcpStateInfo()
    {
        RefreshSnapshotIfNeeded();
        return _iPv6Snapshot;
    }

    internal static void CalculateCount(TcpStateInfo tcpStateInfo, MIB_TCP_STATE state)
    {
        switch (state)
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

    internal void SetGetTcpTableDelegate(GetTcpTableDelegate getTcpTableDelegate) => _getTcpTable = getTcpTableDelegate;
    internal void SetGetTcp6TableDelegate(GetTcpTableDelegate getTcp6TableDelegate) => _getTcp6Table = getTcp6TableDelegate;

    private static IntPtr RetryCalling(GetTcpTableDelegate getTcpTableDelegate)
    {
        const int LoopTimes = 5;
        uint size = 0;
        var result = (uint)NTSTATUS.UnsuccessfulStatus;
        var tcpTable = IntPtr.Zero;

        // Stryker disable once all : Loop for 5 times as a default setting.
        for (int i = 0; i < LoopTimes; ++i)
        {
            // Stryker disable once all : True or false will not affect the results.
            result = getTcpTableDelegate(tcpTable, ref size, false);
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

    private void RefreshSnapshotIfNeeded()
    {
        lock (_lock)
        {
            if (_refreshAfter < TimeProvider.GetUtcNow())
            {
                _iPv4Snapshot = GetSnapshot();
                _iPv6Snapshot = GetIPv6Snapshot();
                _refreshAfter = TimeProvider.GetUtcNow().Add(_samplingInterval);
            }
        }
    }

    private TcpStateInfo GetSnapshot()
    {
        var tcpTable = RetryCalling(_getTcpTable);
        var rawTcpTable = Marshal.PtrToStructure<MIB_TCPTABLE>(tcpTable);

        var tcpStateInfo = new TcpStateInfo();
        var offset = Marshal.OffsetOf<MIB_TCPTABLE>(nameof(MIB_TCPTABLE.Table)).ToInt32();
        var rowPtr = IntPtr.Add(tcpTable, offset);
        for (int i = 0; i < rawTcpTable.NumberOfEntries; ++i)
        {
            var row = Marshal.PtrToStructure<MIB_TCPROW>(rowPtr);
            rowPtr = IntPtr.Add(rowPtr, Marshal.SizeOf<MIB_TCPROW>());

            if (_localIPAddresses.Count > 0 && !_localIPAddresses.Contains(row.LocalAddr))
            {
                continue;
            }

            CalculateCount(tcpStateInfo, row.State);
        }

        Marshal.FreeHGlobal(tcpTable);
        return tcpStateInfo;
    }

    private TcpStateInfo GetIPv6Snapshot()
    {
        var tcpTable = RetryCalling(_getTcp6Table);
        var rawtcpTable = Marshal.PtrToStructure<MIB_TCP6TABLE>(tcpTable);

        var tcpStateInfo = new TcpStateInfo();
        var offset = Marshal.OffsetOf<MIB_TCP6TABLE>(nameof(MIB_TCP6TABLE.Table)).ToInt32();
        var rowPtr = IntPtr.Add(tcpTable, offset);
        for (int i = 0; i < rawtcpTable.NumberOfEntries; ++i)
        {
            var row = Marshal.PtrToStructure<MIB_TCP6ROW>(rowPtr);
            rowPtr = IntPtr.Add(rowPtr, Marshal.SizeOf<MIB_TCP6ROW>());

            if (_iPv6localIPAddresses.Count > 0 && !_iPv6localIPAddresses.Contains(row.LocalAddr.Byte))
            {
                continue;
            }

            CalculateCount(tcpStateInfo, row.State);
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

        /// <summary>
        /// Import of GetTcp6Table win32 function.
        /// </summary>
        /// <param name="pTcpTable">A pointer of a MIB_TCP6TABLE struct.</param>
        /// <param name="pdwSize">On input, specifies the size in bytes of the buffer pointed to by the pTcpTable parameter.
        /// On output, if the buffer is not large enough to hold the returned connection table, the function sets this parameter equal to the required buffer size in bytes.</param>
        /// <param name="bOrder">A Boolean value that specifies whether the TCP connection table should be sorted.</param>
        [DllImport("Iphlpapi.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern uint GetTcp6Table(IntPtr pTcpTable, ref uint pdwSize, bool bOrder);
    }
}

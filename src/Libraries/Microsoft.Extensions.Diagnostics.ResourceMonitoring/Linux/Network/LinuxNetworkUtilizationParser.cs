// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Extensions.ObjectPool;
#if !NET8_0_OR_GREATER
using Microsoft.Shared.StringSplit;
#endif
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Network;

internal class LinuxNetworkUtilizationParser
{
    private static readonly ObjectPool<BufferWriter<char>> _sharedBufferWriterPool = BufferWriterPool.CreateBufferWriterPool<char>();

    /// <remarks>
    /// File that provide information about currently active TCP_IPv4 connections.
    /// </remarks>
    private static readonly FileInfo _tcp = new("/proc/net/tcp");

    /// <remarks>
    /// File that provide information about currently active TCP_IPv6 connections.
    /// </remarks>
    private static readonly FileInfo _tcp6 = new("/proc/net/tcp6");

    private readonly IFileSystem _fileSystem;

    /// <remarks>
    /// Reads the contents of a file located at _tcp4 and parses it to extract information about the TCP/IP state info on the system.
    /// </remarks>
    public TcpStateInfo GetTcpIPv4StateInfo() => GetTcpStateInfo(_tcp);

    /// <remarks>
    /// Reads the contents of a file located at _tcp6 and parses it to extract information about the TCP/IP state info on the system.
    /// </remarks>
    public TcpStateInfo GetTcpIPv6StateInfo() => GetTcpStateInfo(_tcp6);

    public LinuxNetworkUtilizationParser(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <remarks>
    /// The method is used in the LinuxUtilizationParser class to read Span data and calculate the TCP state info.
    /// Refer <see href="https://www.kernel.org/doc/Documentation/networking/proc_net_tcp.txt">proc net tcp</see>.
    /// </remarks>
    [SuppressMessage("Major Code Smell", "S109:Magic numbers should not be used",
        Justification = "We are adding another digit, so we need to multiply by ten.")]
    private static void UpdateTcpStateInfo(ReadOnlySpan<char> buffer, TcpStateInfo tcpStateInfo)
    {
        ReadOnlySpan<char> line = buffer.TrimStart();
        const int Target = 4;

#if NET8_0_OR_GREATER
        Span<Range> range = stackalloc Range[Target];
        int numRanges = line.Split(range, ' ');
#else
        Span<StringRange> range = stackalloc StringRange[Target];
        _ = line.TrySplit(" ", range, out int numRanges, StringComparison.OrdinalIgnoreCase, StringSplitOptions.RemoveEmptyEntries);
#endif
        if (numRanges < Target)
        {
            Throw.InvalidOperationException($"Could not split contents. We expected every line to contain more than {Target - 1} elements, but it has only {numRanges} elements.");
        }

#if NET8_0_OR_GREATER
        ReadOnlySpan<char> tcpConnectionState = line.Slice(range[Target - 1].Start.Value, range[Target - 1].End.Value - range[Target - 1].Start.Value);
#else
        ReadOnlySpan<char> tcpConnectionState = line.Slice(range[Target - 1].Index, range[Target - 1].Count);
#endif

        // until this API proposal is implemented https://github.com/dotnet/runtime/issues/61397
        // we have to allocate & throw away memory using ToString():
        switch ((LinuxTcpState)Convert.ToInt32(tcpConnectionState.ToString(), 16))
        {
            case LinuxTcpState.ESTABLISHED:
                tcpStateInfo.EstabCount++;
                break;
            case LinuxTcpState.SYN_SENT:
                tcpStateInfo.SynSentCount++;
                break;
            case LinuxTcpState.SYN_RECV:
                tcpStateInfo.SynRcvdCount++;
                break;
            case LinuxTcpState.FIN_WAIT1:
                tcpStateInfo.FinWait1Count++;
                break;
            case LinuxTcpState.FIN_WAIT2:
                tcpStateInfo.FinWait2Count++;
                break;
            case LinuxTcpState.TIME_WAIT:
                tcpStateInfo.TimeWaitCount++;
                break;
            case LinuxTcpState.CLOSE:
                tcpStateInfo.ClosedCount++;
                break;
            case LinuxTcpState.CLOSE_WAIT:
                tcpStateInfo.CloseWaitCount++;
                break;
            case LinuxTcpState.LAST_ACK:
                tcpStateInfo.LastAckCount++;
                break;
            case LinuxTcpState.LISTEN:
                tcpStateInfo.ListenCount++;
                break;
            case LinuxTcpState.CLOSING:
                tcpStateInfo.ClosingCount++;
                break;
            default:
                throw new InvalidEnumArgumentException($"Cannot find status: {tcpConnectionState} in LinuxTcpState");
        }
    }

    /// <remarks>
    /// Reads the contents of a file located at file and parses it to extract information about the TCP/IP state info on the system.
    /// </remarks>
    private TcpStateInfo GetTcpStateInfo(FileInfo file)
    {
        // The value we are interested in starts with this. We just want to make sure it is true.
        const string Sl = "sl";
        var tcpStateInfo = new TcpStateInfo();
        using ReturnableBufferWriter<char> bufferWriter = new(_sharedBufferWriterPool);
        using var enumerableLines = _fileSystem.ReadAllByLines(file, bufferWriter.Buffer).GetEnumerator();
        if (!enumerableLines.MoveNext())
        {
            Throw.InvalidOperationException($"Could not parse '{file}'. File was empty.");
        }

        var firstLine = enumerableLines.Current.TrimStart().Span;
        if (!firstLine.StartsWith(Sl, StringComparison.Ordinal))
        {
            Throw.InvalidOperationException($"Could not parse '{file}'. We expected first line of the file to start with '{Sl}' but it was '{firstLine.ToString()}' instead.");
        }

        while (enumerableLines.MoveNext())
        {
            UpdateTcpStateInfo(enumerableLines.Current.Span, tcpStateInfo);
        }

        return tcpStateInfo;
    }
}

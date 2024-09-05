// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.ObjectPool;
#if !NET8_0_OR_GREATER
using Microsoft.Shared.StringSplit;
#endif
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Pools;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Network;

internal sealed class LinuxNetworkUtilizationParser
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
    /// Reads the contents of a file located at <see cref="_tcp"/> and parses it to extract information about the TCP/IP state info of the system.
    /// </remarks>
    public TcpStateInfo GetTcpIPv4StateInfo() => GetTcpStateInfo(_tcp);

    /// <remarks>
    /// Reads the contents of a file located at <see cref="_tcp6"/> and parses it to extract information about the TCP/IP state info of the system.
    /// </remarks>
    public TcpStateInfo GetTcpIPv6StateInfo() => GetTcpStateInfo(_tcp6);

    public LinuxNetworkUtilizationParser(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <remarks>
    /// Parses the contents of the <paramref name="buffer"/> and updates the <paramref name="tcpStateInfo"/> with the parsed information.
    /// For the data format expected in the <paramref name="buffer"/>, refer <see href="https://www.kernel.org/doc/Documentation/networking/proc_net_tcp.txt">proc net tcp</see>.
    /// </remarks>
    private static void UpdateTcpStateInfo(ReadOnlySpan<char> buffer, TcpStateInfo tcpStateInfo)
    {
        const int Base16 = 16;

        // The buffer contains one line from /proc/net/tcp(6) file, e.g.:
        //   0: 030011AC:8AF2 C1B17822:01BB 01 00000000:00000000 02:000000D1 00000000   472        0 2481276 2 00000000c62511cb 28 4 26 10 -1
        // The line may contain leading spaces, so we have to trim those.
        // tcpConnectionState is in the 4th column - i.e., "01".
        ReadOnlySpan<char> line = buffer.TrimStart();

#if NET8_0_OR_GREATER
        const int Target = 5;
        Span<Range> range = stackalloc Range[Target];

        // In .NET 8+, if capacity of destination range array is less than number of ranges found by ReadOnlySpan<T>.Split(),
        // the last range in the array will get all the remaining elements of the ReadOnlySpan.
        // Therefore, we request 5 ranges instead of 4, and then range[Target - 2] will have the range we need without the remaining elements.
        int numRanges = line.Split(range, ' ', StringSplitOptions.RemoveEmptyEntries);
#else
        const int Target = 4;
        Span<StringRange> range = stackalloc StringRange[Target];

        // In our StringRange API, if capacity of destination range array is less than number of ranges found by ReadOnlySpan<T>.TrySplit(),
        // the last range in the array will get the last range as expected, and all remaining elements will be ignored.
        // Hence range[Target - 1] will have the last range as we need.
        _ = line.TrySplit(" ", range, out int numRanges, StringComparison.OrdinalIgnoreCase, StringSplitOptions.RemoveEmptyEntries);
#endif
        if (numRanges < Target)
        {
            Throw.InvalidOperationException($"Could not split contents. We expected every line to contain more than {Target - 1} elements, but it has only {numRanges} elements.");
        }

#if NET8_0_OR_GREATER
        ReadOnlySpan<char> tcpConnectionState = line.Slice(range[Target - 2].Start.Value, range[Target - 2].End.Value - range[Target - 2].Start.Value);
#else
        ReadOnlySpan<char> tcpConnectionState = line.Slice(range[Target - 1].Index, range[Target - 1].Count);
#endif

        // At this point, tcpConnectionState contains one of TCP connection states in hexadecimal format, e.g., "01",
        // which we now need to convert to the LinuxTcpState enum.
        // Note: until this API proposal is implemented https://github.com/dotnet/runtime/issues/61397
        // we have to allocate and throw away memory using .ToString().
        var state = (LinuxTcpState)Convert.ToInt32(tcpConnectionState.ToString(), Base16);
        switch (state)
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
                Throw.IfOutOfRange(state);
                break;
        }
    }

    /// <remarks>
    /// Reads the contents of a file and parses it to extract information about the TCP/IP state info of the system.
    /// </remarks>
    private TcpStateInfo GetTcpStateInfo(FileInfo file)
    {
        // The value we are interested in starts with this "sl".
        const string Sl = "sl";
        TcpStateInfo tcpStateInfo = new();
        using ReturnableBufferWriter<char> bufferWriter = new(_sharedBufferWriterPool);
        using IEnumerator<ReadOnlyMemory<char>> enumerableLines = _fileSystem.ReadAllByLines(file, bufferWriter.Buffer).GetEnumerator();
        if (!enumerableLines.MoveNext())
        {
            Throw.InvalidOperationException($"Could not parse '{file}'. File was empty.");
        }

        ReadOnlySpan<char> firstLine = enumerableLines.Current.TrimStart().Span;
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

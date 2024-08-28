// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Network;
using VerifyXunit;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

[UsesVerify]
public sealed class LinuxNetworkUtilizationParserTests
{
    private const string VerifiedDataDirectory = "Verified";

    [Theory]
    [InlineData("DFIJEUWGHFWGBWEFWOMDOWKSLA")]
    [InlineData("")]
    [InlineData("________________________Asdasdasdas          dd")]
    [InlineData(" ")]
    [InlineData("!@#!$%!@")]
    public async Task Parser_Throws_When_Data_Is_Invalid(string line)
    {
        var parser = new LinuxNetworkUtilizationParser(new HardcodedValueFileSystem(line));

        await Verifier.Verify(Record.Exception(parser.GetTcpIPv4StateInfo)).UseParameters(line).UseMethodName("ipv4").UseDirectory(VerifiedDataDirectory);
        await Verifier.Verify(Record.Exception(parser.GetTcpIPv4StateInfo)).UseParameters(line).UseMethodName("ipv6").UseDirectory(VerifiedDataDirectory);
    }

    [Fact]
    public async Task Parser_Tcp_State_Info_Exception()
    {
        var fileSystem = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            {
                new FileInfo("/proc/net/tcp"),
                "  sl  local_address rem_address   st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode\r\n" +
                "   0: 030011AC:8AF2\r\n"
            },
            {
                new FileInfo("/proc/net/tcp6"),
                "  sl  local_address                         remote_address                        st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode\r\n" +
                "   0: 00000000000000000000000000000000:0BB8 00000000000000000000000000000000:0000\r\n"
            },
        });

        var parser = new LinuxNetworkUtilizationParser(fileSystem);

        // Expected every line to contain more than 4 elements, but it has only 2 elements.
        await Verifier.Verify(Record.Exception(parser.GetTcpIPv4StateInfo)).UniqueForRuntimeAndVersion().UseMethodName("1").UseDirectory(VerifiedDataDirectory);

        // Expected every line to contain more than 4 elements, but it has only 3 elements.
        await Verifier.Verify(Record.Exception(parser.GetTcpIPv4StateInfo)).UniqueForRuntimeAndVersion().UseMethodName("2").UseDirectory(VerifiedDataDirectory);

        string tcpFile =
            "  sl  local_address rem_address   st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode\r\n" +
            "   0: 030011AC:8AF2 C1B17822:01BB 00 00000000:00000000 02:000000D1 00000000   472        0 2481276 2 00000000c62511cb 28 4 26 10 -1\r\n";
        string tcp6File =
            "  sl  local_address                         remote_address                        st tx_queue rx_queue tr tm->when retrnsmt   uid  timeout inode\r\n" +
            "   0: 00000000000000000000000000000000:0BBB 00000000000000000000000000000000:0003 0C 00000000:00000000 00:00000000 00000000   472        0 2455375 1 00000000f4cb7621 100 0 0 10 0\r\n";

        fileSystem.ReplaceFileContent(new FileInfo("/proc/net/tcp"), tcpFile);
        fileSystem.ReplaceFileContent(new FileInfo("/proc/net/tcp6"), tcp6File);

        // Cannot find status: 00 in LINUX_TCP_STATE
        await Verifier.Verify(Record.Exception(parser.GetTcpIPv4StateInfo)).UseMethodName("3").UseDirectory(VerifiedDataDirectory);

        // Cannot find status: 12 in LINUX_TCP_STATE
        await Verifier.Verify(Record.Exception(parser.GetTcpIPv6StateInfo)).UseMethodName("4").UseDirectory(VerifiedDataDirectory);

        tcpFile = "";
        tcp6File = "";

        fileSystem.ReplaceFileContent(new FileInfo("/proc/net/tcp"), tcpFile);
        fileSystem.ReplaceFileContent(new FileInfo("/proc/net/tcp6"), tcp6File);

        // Could not parse '/proc/net/tcp'. File was empty.
        await Verifier.Verify(Record.Exception(parser.GetTcpIPv4StateInfo)).UseMethodName("5").UseDirectory(VerifiedDataDirectory);

        // Could not parse '/proc/net/tcp6'. File was empty.
        await Verifier.Verify(Record.Exception(parser.GetTcpIPv6StateInfo)).UseMethodName("6").UseDirectory(VerifiedDataDirectory);
    }

    [Fact]
    public void Parser_Tcp_State_Info()
    {
        var parser = new LinuxNetworkUtilizationParser(new FileNamesOnlyFileSystem(TestResources.TestFilesLocation));
        TcpStateInfo tcp4StateInfo = parser.GetTcpIPv4StateInfo();

        Assert.Equal(2, tcp4StateInfo.EstabCount);
        Assert.Equal(1, tcp4StateInfo.SynSentCount);
        Assert.Equal(1, tcp4StateInfo.SynRcvdCount);
        Assert.Equal(1, tcp4StateInfo.FinWait1Count);
        Assert.Equal(0, tcp4StateInfo.FinWait2Count);
        Assert.Equal(3, tcp4StateInfo.TimeWaitCount);
        Assert.Equal(2, tcp4StateInfo.ClosedCount);
        Assert.Equal(2, tcp4StateInfo.CloseWaitCount);
        Assert.Equal(1, tcp4StateInfo.LastAckCount);
        Assert.Equal(1, tcp4StateInfo.ListenCount);
        Assert.Equal(1, tcp4StateInfo.ClosingCount);

        var tcp6StateInfo = parser.GetTcpIPv6StateInfo();
        Assert.Equal(4, tcp6StateInfo.EstabCount);
        Assert.Equal(1, tcp6StateInfo.SynSentCount);
        Assert.Equal(1, tcp6StateInfo.SynRcvdCount);
        Assert.Equal(1, tcp6StateInfo.FinWait1Count);
        Assert.Equal(1, tcp6StateInfo.FinWait2Count);
        Assert.Equal(1, tcp6StateInfo.TimeWaitCount);
        Assert.Equal(1, tcp6StateInfo.ClosedCount);
        Assert.Equal(1, tcp6StateInfo.CloseWaitCount);
        Assert.Equal(1, tcp6StateInfo.LastAckCount);
        Assert.Equal(1, tcp6StateInfo.ListenCount);
        Assert.Equal(1, tcp6StateInfo.ClosingCount);
    }
}

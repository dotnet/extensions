// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Shared.Pools;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

[OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
public sealed class OSFileSystemTests
{
    [ConditionalFact]
    public void Reading_First_File_Line_Works()
    {
        const string Content = "Name:   cat";
        var fileSystem = new OSFileSystem();
        var file = new FileInfo("fixtures/status");
        var bw = new BufferWriter<char>();
        fileSystem.ReadFirstLine(file, bw);
        var s = new string(bw.WrittenSpan).Replace("\r", ""); // Git is overwriting LF to CRLF all the time for windows, I am so tired of it I am hacking it!!

        Assert.Equal(Content, s);
    }

    [ConditionalFact]
    public void Reading_The_Whole_File_Works()
    {
        const string Content = "user 1399428\nsystem 1124053\n";
        var fileSystem = new OSFileSystem();
        var file = new FileInfo("fixtures/cpuacct.stat");
        var bw = new BufferWriter<char>();
        fileSystem.ReadAll(file, bw);

        var s = new string(bw.WrittenSpan).Replace("\r", ""); // Git is overwriting LF to CRLF all the time for windows, I am so tired of it I am hacking it!!

        Assert.Equal(Content, s);
    }

    [ConditionalTheory]
    [InlineData(128)]
    [InlineData(256)]
    [InlineData(512)]
    [InlineData(1024)]
    public void Reading_Small_Portion_Of_Big_File_Works(int length)
    {
        const char Content = 'R';
        var b = new char[length];
        var fileSystem = new OSFileSystem();
        var file = new FileInfo("fixtures/FileWithRChars");
        var written = fileSystem.Read(file, length, b);

        Assert.True(length >= written);
        Assert.True(b.AsSpan(0, written).SequenceEqual(new string(Content, written).AsSpan()));
    }
}

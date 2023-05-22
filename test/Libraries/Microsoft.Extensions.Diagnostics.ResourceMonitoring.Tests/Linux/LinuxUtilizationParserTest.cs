// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Internal;
using Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;
using Microsoft.TestUtilities;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

[OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Windows specific.")]
public sealed class LinuxUtilizationParserTest
{
    [ConditionalTheory]
    [InlineData("DFIJEUWGHFWGBWEFWOMDOWKSLA")]
    [InlineData("")]
    [InlineData("________________________Asdasdasdas          dd")]
    [InlineData(" ")]
    [InlineData("!@#!$%!@")]
    public void Parser_Throws_When_Data_Is_Invalid(string line)
    {
        var parser = new LinuxUtilizationParser(new HardcodedValueFileSystem(line), new FakeUserHz(100));

        Assert.Throws<InvalidOperationException>(() => parser.GetHostAvailableMemory());
        Assert.Throws<InvalidOperationException>(() => parser.GetAvailableMemoryInBytes());
        Assert.Throws<InvalidOperationException>(() => parser.GetMemoryUsageInBytes());
        Assert.Throws<InvalidOperationException>(() => parser.GetCgroupLimitedCpus());
        Assert.Throws<InvalidOperationException>(() => parser.GetHostCpuUsageInNanoseconds());
        Assert.Throws<InvalidOperationException>(() => parser.GetHostCpuCount());
        Assert.Throws<InvalidOperationException>(() => parser.GetCgroupCpuUsageInNanoseconds());
    }

    [ConditionalFact]
    public void Parser_Can_Read_Host_And_Cgroup_Available_Cpu_Count()
    {
        var parser = new LinuxUtilizationParser(new FileNamesOnlyFileSystem(TestResources.TestFilesLocation), new FakeUserHz(100));
        var hostCpuCount = parser.GetHostCpuCount();
        var cgroupCpuCount = parser.GetCgroupLimitedCpus();

        Assert.Equal(2.0, hostCpuCount);
        Assert.Equal(1.0, cgroupCpuCount);
    }

    [ConditionalFact]
    public void Parser_Provides_Total_Available_Memory_In_Bytes()
    {
        var fs = new FileNamesOnlyFileSystem(TestResources.TestFilesLocation);
        var parser = new LinuxUtilizationParser(fs, new FakeUserHz(100));

        var totalMem = parser.GetHostAvailableMemory();

        Assert.Equal(16_233_760UL * 1024, totalMem);
    }

    [ConditionalTheory]
    [InlineData("----------------------")]
    [InlineData("@ @#dddada")]
    [InlineData("1231234124124")]
    [InlineData("1024 KB")]
    [InlineData("1024 KB  d \n\r 1024")]
    [InlineData("\n\r")]
    [InlineData("")]
    [InlineData("Suspicious")]
    [InlineData("string@")]
    [InlineData("string12312")]
    [InlineData("total-inactive-file")]
    [InlineData("total_inactive-file")]
    [InlineData("total_active_file")]
    [InlineData("total_inactive_file:_ 213912")]
    [InlineData("Total_Inactive_File 2")]
    public void When_Calling_GetMemoryUsageInBytes_Parser_Throws_When_MemoryStat_Doesnt_Contain_Total_Inactive_File_Section(string content)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory/memory.stat"), content }
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetMemoryUsageInBytes());

        Assert.IsAssignableFrom<InvalidOperationException>(r);
        Assert.Contains("/sys/fs/cgroup/memory/memory.stat", r.Message);
        Assert.Contains("total_inactive_file", r.Message);
    }

    [ConditionalTheory]
    [InlineData("----------------------")]
    [InlineData("@ @#dddada")]
    [InlineData("_1231234124124")]
    [InlineData("eee 1024 KB")]
    [InlineData("\n\r")]
    [InlineData("")]
    [InlineData("Suspicious")]
    [InlineData("Suspicious12312312")]
    [InlineData("string@")]
    [InlineData("string12312")]
    public void When_Calling_GetMemoryUsageInBytes_Parser_Throws_When_UsageInBytes_Doesnt_Contain_Just_A_Number(string content)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory/memory.stat"), "total_inactive_file 0" },
            { new FileInfo("/sys/fs/cgroup/memory/memory.usage_in_bytes"), content }
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetMemoryUsageInBytes());

        Assert.IsAssignableFrom<InvalidOperationException>(r);
        Assert.Contains("/sys/fs/cgroup/memory/memory.usage_in_bytes", r.Message);
    }

    [ConditionalTheory]
    [InlineData(10, 1)]
    [InlineData(23, 22)]
    [InlineData(100000, 10000)]
    public void When_Calling_GetMemoryUsageInBytes_Parser_Throws_When_Inactive_Memory_Is_Bigger_Than_Total_Memory(int inactive, int total)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory/memory.stat"), $"total_inactive_file {inactive}" },
            { new FileInfo("/sys/fs/cgroup/memory/memory.usage_in_bytes"), total.ToString(CultureInfo.CurrentCulture) }
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetMemoryUsageInBytes());

        Assert.IsAssignableFrom<InvalidOperationException>(r);
        Assert.Contains("lesser than", r.Message);
    }

    [ConditionalTheory]
    [InlineData("Mem")]
    [InlineData("MemTotal:")]
    [InlineData("MemTotal: 120")]
    [InlineData("MemTotal: kb")]
    [InlineData("MemTotal: KB")]
    [InlineData("MemTotal: PB")]
    [InlineData("MemTotal: 1024 PB")]
    [InlineData("MemTotal: 1024   ")]
    [InlineData("MemTotal: 1024 @@  ")]
    [InlineData("MemoryTotal: 1024 MB ")]
    [InlineData("MemoryTotal: 123123123123123123")]
    public void When_Calling_GetHostAvailableMemory_Parser_Throws_When_MemInfo_Does_Not_Contain_TotalMemory(string totalMemory)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/meminfo"), totalMemory },
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetHostAvailableMemory());

        Assert.IsAssignableFrom<InvalidOperationException>(r);
        Assert.Contains("/proc/meminfo", r.Message);
    }

    [ConditionalTheory]
    [InlineData("kB", 231, 236544)]
    [InlineData("MB", 287, 300_941_312)]
    [InlineData("GB", 372, 399_431_958_528)]
    [InlineData("TB", 2, 219_902_325_555_2)]
    [SuppressMessage("Critical Code Smell", "S3937:Number patterns should be regular", Justification = "Its OK.")]
    public void When_Calling_GetHostAvailableMemory_Parser_Correctly_Transforms_Supported_Units_To_Bytes(string unit, int value, ulong bytes)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/meminfo"), $"MemTotal: {value} {unit}" },
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var memory = p.GetHostAvailableMemory();

        Assert.Equal(bytes, memory);
    }

    [ConditionalFact]
    public void When_No_Cgroup_Cpu_Limits_Are_Not_Set_We_Get_Available_Cpus_From_CpuSetCpus()
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpuset/cpuset.cpus"), $"0-11" },
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_quota_us"), "-1" },
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_period_us"), "-1" }
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var cpus = p.GetCgroupLimitedCpus();

        Assert.Equal(12, cpus);
    }

    [ConditionalTheory]
    [InlineData("-11")]
    [InlineData("0-")]
    [InlineData("d-22")]
    [InlineData("22-d")]
    [InlineData("22-18")]
    [InlineData("aaaa")]
    [InlineData("    d  182-1923")]
    [InlineData("")]
    [InlineData("1-18-22")]
    [InlineData("1-18                   \r\n")]
    [InlineData("\r\n")]
    public void Parser_Throws_When_CpuSet_Has_Invalid_Content(string content)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpuset/cpuset.cpus"), content },
                        { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_quota_us"), "12" },
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_period_us"), "-1" }
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetCgroupLimitedCpus());

        Assert.IsAssignableFrom<InvalidOperationException>(r);
        Assert.Contains("/sys/fs/cgroup/cpuset/cpuset.cpus", r.Message);
    }

    [ConditionalTheory]
    [InlineData("-1", "18")]
    [InlineData("18", "-1")]
    [InlineData("18", "")]
    [InlineData("", "18")]
    public void When_Quota_And_Period_Are_Minus_One_It_Fallbacks_To_Cpuset(string quota, string period)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpuset/cpuset.cpus"), "@" },
                        { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_quota_us"), quota },
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_period_us"), period }
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetCgroupLimitedCpus());

        Assert.IsAssignableFrom<InvalidOperationException>(r);
        Assert.Contains("/sys/fs/cgroup/cpuset/cpuset.cpus", r.Message);
    }

    [Theory]
    [InlineData("dd1d", "18")]
    [InlineData("-18", "18")]
    [InlineData("\r\r\r\r\r", "18")]
    [InlineData("123", "\r\r\r\r\r")]
    [InlineData("-", "d'")]
    [InlineData("-", "d/:")]
    [InlineData("2", "d/:")]
    [InlineData("2d2d2d", "e3")]
    [InlineData("3d", "d3")]
    [InlineData("           12", "eeeee 12")]
    [InlineData("1 2", "eeeee 12")]
    [InlineData("12       ", "")]
    public void Parser_Throws_When_Cgroup_Cpu_Files_Contain_Invalid_Data(string quota, string period)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_quota_us"), quota },
            { new FileInfo("/sys/fs/cgroup/cpu/cpu.cfs_period_us"), period }
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetCgroupLimitedCpus());

        Assert.IsAssignableFrom<InvalidOperationException>(r);
        Assert.Contains("/sys/fs/cgroup/cpu/cpu.cfs_", r.Message);
    }

    [Fact]
    public void ReadingCpuUsage_Does_Not_Throw_For_Valid_Input()
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/stat"), "cpu  2569530 36700 245693 4860924 82283 0 4360 0 0 0" }
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetHostCpuUsageInNanoseconds());

        Assert.Null(r);
    }

    [Fact]
    public void ReadingTotalMemory_Does_Not_Throw_For_Valid_Input()
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory/memory.usage_in_bytes"), "32493514752\r\n" },
            { new FileInfo("/sys/fs/cgroup/memory/memory.stat"), "total_inactive_file 100" }
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetMemoryUsageInBytes());

        Assert.Null(r);
    }

    [Theory]
    [InlineData("2569530367000")]
    [InlineData("  2569530 36700 245693 4860924 82283 0 4360 0dsa 0 0 asdasd @@@@")]
    [InlineData("asdasd  2569530 36700 245693 4860924 82283 0 4360 0 0 0")]
    [InlineData("  2569530 36700 245693")]
    [InlineData("cpu  2569530 36700 245693")]
    [InlineData("  2")]
    public void ReadingCpuUsage_Does_Throws_For_Valid_Input(string content)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/stat"), content }
        });

        var p = new LinuxUtilizationParser(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetHostCpuUsageInNanoseconds());

        Assert.IsAssignableFrom<InvalidOperationException>(r);
        Assert.Contains("proc/stat", r.Message);
    }
}

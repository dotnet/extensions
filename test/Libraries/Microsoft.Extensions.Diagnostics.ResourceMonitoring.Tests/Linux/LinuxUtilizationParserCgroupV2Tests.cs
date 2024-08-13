﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Shared.Pools;
using Microsoft.TestUtilities;
using Moq;
using VerifyXunit;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

[OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX, SkipReason = "Linux specific tests")]
[UsesVerify]
public sealed class LinuxUtilizationParserCgroupV2Tests
{
    private const string VerifiedDataDirectory = "Verified";

    [ConditionalTheory]
    [InlineData("DFIJEUWGHFWGBWEFWOMDOWKSLA")]
    [InlineData("")]
    [InlineData("________________________Asdasdasdas          dd")]
    [InlineData(" ")]
    [InlineData("!@#!$%!@")]
    public void Throws_When_Data_Is_Invalid(string line)
    {
        var parser = new LinuxUtilizationParserCgroupV2(new HardcodedValueFileSystem(line), new FakeUserHz(100));

        Assert.Throws<InvalidOperationException>(() => parser.GetHostAvailableMemory());
        Assert.Throws<InvalidOperationException>(() => parser.GetAvailableMemoryInBytes());
        Assert.Throws<InvalidOperationException>(() => parser.GetMemoryUsageInBytes());
        Assert.Throws<InvalidOperationException>(() => parser.GetCgroupLimitedCpus());
        Assert.Throws<InvalidOperationException>(() => parser.GetHostCpuUsageInNanoseconds());
        Assert.Throws<InvalidOperationException>(() => parser.GetHostCpuCount());
        Assert.Throws<InvalidOperationException>(() => parser.GetCgroupCpuUsageInNanoseconds());
        Assert.Throws<InvalidOperationException>(() => parser.GetCgroupRequestCpu());
    }

    [ConditionalFact]
    public void Can_Read_Host_And_Cgroup_Available_Cpu_Count()
    {
        var parser = new LinuxUtilizationParserCgroupV2(new FileNamesOnlyFileSystem(TestResources.TestFilesLocation), new FakeUserHz(100));
        var hostCpuCount = parser.GetHostCpuCount();
        var cgroupCpuCount = parser.GetCgroupLimitedCpus();

        Assert.Equal(2.0, hostCpuCount);
        Assert.Equal(2.0, cgroupCpuCount);
    }

    [ConditionalFact]
    public void Provides_Total_Available_Memory_In_Bytes()
    {
        var fs = new FileNamesOnlyFileSystem(TestResources.TestFilesLocation);
        var parser = new LinuxUtilizationParserCgroupV2(fs, new FakeUserHz(100));

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
    [InlineData("Total_Inactive_File 2")]
    [InlineData("total_inactive_file:_ 21391")]
    [InlineData("string@ -1")]
    public Task Throws_When_TotalInactiveFile_Is_Invalid(string content)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory.stat"), content }
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetMemoryUsageInBytes());

        return Verifier.Verify(r).UseParameters(content).UseDirectory(VerifiedDataDirectory);
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
    public Task Throws_When_UsageInBytes_Is_Invalid(string content)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory.stat"), "inactive_file 14340" },
            { new FileInfo("/sys/fs/cgroup/memory.current"), content }
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetMemoryUsageInBytes());

        return Verifier.Verify(r).UseParameters(content).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalTheory]
    [InlineData("max\n", 134_796_910_592ul)]
    [InlineData("1000000\n", 1_000_000ul)]
    public void Returns_Available_Memory_When_AvailableMemoryInBytes_Is_Valid(string content, ulong expectedResult)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory.max"), content },
            { new FileInfo("/proc/meminfo"), "MemTotal:       131637608 kB" }
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var result = p.GetAvailableMemoryInBytes();

        Assert.Equal(expectedResult, result);
    }

    [ConditionalTheory]
    [InlineData("Suspicious12312312")]
    [InlineData("string@")]
    [InlineData("string12312")]
    public Task Throws_When_AvailableMemoryInBytes_Doesnt_Contain_Just_A_Number(string content)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory.max"), content },
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetAvailableMemoryInBytes());

        return Verifier.Verify(r).UseParameters(content).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalFact]
    public Task Throws_When_UsageInBytes_Doesnt_Contain_A_Number()
    {
        var regexPatternforSlices = @"\w+.slice";
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/system.slice/memory.current"), "dasda"},
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetMemoryUsageInBytesFromSlices(regexPatternforSlices));

        return Verifier.Verify(r).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalFact]
    public void Returns_Memory_Usage_When_Memory_Usage_Is_Valid()
    {
        var regexPatternforSlices = @"\w+.slice";
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/system.slice/memory.current"), "5342342"},
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = p.GetMemoryUsageInBytesFromSlices(regexPatternforSlices);

        Assert.Equal(5_342_342, r);
    }

    [ConditionalTheory]
    [InlineData(104343, 1)]
    [InlineData(23423, 22)]
    [InlineData(10000, 100)]
    public Task Throws_When_Inactive_Memory_Is_Bigger_Than_Total_Memory(int inactive, int total)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory.stat"), $"inactive_file {inactive}" },
            { new FileInfo("/sys/fs/cgroup/memory.current"), total.ToString(CultureInfo.CurrentCulture) }
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetMemoryUsageInBytes());

        return Verifier.Verify(r).UseParameters(inactive, total).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalTheory]
    [InlineData("Mem")]
    [InlineData("MemTotal:")]
    [InlineData("MemTotal: 120")]
    [InlineData("MemTotal: kb")]
    [InlineData("MemTotal: MB")]
    [InlineData("MemTotal: PB")]
    [InlineData("MemTotal: 1024 PB")]
    [InlineData("MemTotal: 1024   ")]
    [InlineData("MemTotal: 1024 @@  ")]
    [InlineData("MemoryTotal: 1024 MB ")]
    [InlineData("MemoryTotal: 123123123123123123")]
    public Task Throws_When_MemInfo_Does_Not_Contain_TotalMemory(string totalMemory)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/meminfo"), totalMemory },
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetHostAvailableMemory());

        return Verifier.Verify(r).UseParameters(totalMemory).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalTheory]
    [InlineData("kB", 231, 236_544)]
    [InlineData("MB", 287, 300_941_312)]
    [InlineData("GB", 372, 399_431_958_528)]
    [InlineData("TB", 2, 2_199_023_255_552)]
    public void Transforms_Supported_Units_To_Bytes(string unit, int value, ulong bytes)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/meminfo"), $"MemTotal: {value} {unit}" },
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var memory = p.GetHostAvailableMemory();

        Assert.Equal(bytes, memory);
    }

    [ConditionalTheory]
    [InlineData("0-11", 12)]
    [InlineData("0", 1)]
    [InlineData("1000", 1)]
    [InlineData("0,1", 2)]
    [InlineData("0,1,2", 3)]
    [InlineData("0,1,2,4", 4)]
    [InlineData("0,1-2,3", 4)]
    [InlineData("0,1,2-3,4", 5)]
    [InlineData("0-1,2-3", 4)]
    [InlineData("0-1,2-3,4-5", 6)]
    [InlineData("0-2,3-5,6-8", 9)]
    public void Gets_Available_Cpus_From_CpuSetCpus_When_Cpu_Limits_Not_Set(string content, int result)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), content },
            { new FileInfo("/sys/fs/cgroup/cpu.max"), "-1" },
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var cpus = p.GetCgroupLimitedCpus();

        Assert.Equal(result, cpus);
    }

    [ConditionalFact]
    public void Gets_Available_Cpus_From_CpuSetCpus_When_Cpu_Max_Set_To_Max_()
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "0,1,2" },
            { new FileInfo("/sys/fs/cgroup/cpu.max"), "max 100000" },
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var cpus = p.GetCgroupLimitedCpus();

        Assert.Equal(3, cpus);
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
    public Task Throws_When_CpuSet_Has_Invalid_Content(string content)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), content },
            { new FileInfo("/sys/fs/cgroup/cpu.max"), "-1" }
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetHostCpuCount());

        return Verifier.Verify(r).UseParameters(content).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalFact]
    public Task Fallsback_To_Cpuset_When_Quota_And_Period_Are_Minus_One_()
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpuset.cpus.effective"), "@" },
            { new FileInfo("/sys/fs/cgroup/cpu.max"), "-1" }
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetCgroupLimitedCpus());

        return Verifier.Verify(r).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalTheory]
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
    [InlineData("12       ", "")]
    public Task Throws_When_Cgroup_Cpu_Files_Contain_Invalid_Data(string quota, string period)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpu.max"), $"{quota} {period}"},
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetCgroupLimitedCpus());

        return Verifier.Verify(r).UseParameters(quota, period).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalFact]
    public void Reads_CpuUsage_When_Valid_Input()
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/stat"), "cpu  2569530 36700 245693 4860924 82283 0 4360 0 0 0" }
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = p.GetHostCpuUsageInNanoseconds();

        Assert.Equal(77_994_900_000_000, r);
    }

    [ConditionalFact]
    public void Reads_TotalMemory_When_Valid_Input()
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/memory.current"), "32493514752" },
            { new FileInfo("/sys/fs/cgroup/memory.stat"), "inactive_file 100" }
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetMemoryUsageInBytes());

        Assert.Null(r);
    }

    [ConditionalTheory]
    [InlineData("2569530367000")]
    [InlineData("  2569530 36700 245693 4860924 82283 0 4360 0dsa")]
    [InlineData("asdasd  2569530 36700 245693 4860924 82283 0 4360 0 0 0")]
    [InlineData("  2569530 36700 245693")]
    [InlineData("cpu  2569530 36700 245693")]
    [InlineData("  2")]
    public Task Throws_When_CpuUsage_Invalid(string content)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/stat"), content }
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetHostCpuUsageInNanoseconds());

        return Verifier.Verify(r).UseParameters(content).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalTheory]
    [InlineData("usage_", 12222)]
    [InlineData("dasd", -1)]
    [InlineData("@#dddada", 342322)]
    public Task Throws_When_CpuAcctUsage_Has_Invalid_Content_Both_Parts(string content, int value)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpu.stat"), $"{content} {value}"},
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetCgroupCpuUsageInNanoseconds());

        return Verifier.Verify(r).UseParameters(content, value).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalTheory]
    [InlineData(-32131)]
    [InlineData(-1)]
    [InlineData(-15.323)]
    public Task Throws_When_Usage_Usec_Has_Negative_Value(int value)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpu.stat"), $"usage_usec {value}"},
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetCgroupCpuUsageInNanoseconds());

        return Verifier.Verify(r).UseParameters(value).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalTheory]
    [InlineData("-1")]
    [InlineData("dasrz3424")]
    [InlineData("0")]
    [InlineData("10001")]
    public Task Throws_When_Cgroup_Cpu_Weight_Files_Contain_Invalid_Data(string content)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpu.weight"), content },
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Record.Exception(() => p.GetCgroupRequestCpu());

        return Verifier.Verify(r).UseParameters(content).UseDirectory(VerifiedDataDirectory);
    }

    [ConditionalTheory]
    [InlineData("2500", 64.0)]
    [InlineData("10000", 256.0)]
    public void Calculates_Cpu_Request_From_Cpu_Weight(string content, float result)
    {
        var f = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/sys/fs/cgroup/cpu.weight"), content },
        });

        var p = new LinuxUtilizationParserCgroupV2(f, new FakeUserHz(100));
        var r = Math.Round(p.GetCgroupRequestCpu());

        Assert.Equal(result, r);
    }

    [ConditionalFact]
    public async Task Is_Thread_Safe_Async()
    {
        var f1 = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/stat"), "cpu  6163 0 3853 4222848 614 0 1155 0 0 0\r\ncpu0 240 0 279 210987 59 0 927 0 0 0" },
        });
        var f2 = new HardcodedValueFileSystem(new Dictionary<FileInfo, string>
        {
            { new FileInfo("/proc/stat"), "cpu  9137 0 9296 13972503 1148 0 2786 0 0 0\r\ncpu0 297 0 431 698663 59 0 2513 0 0 0" },
        });

        int callCount = 0;
        Mock<IFileSystem> fs = new();
        fs.Setup(x => x.ReadFirstLine(It.IsAny<FileInfo>(), It.IsAny<BufferWriter<char>>()))
             .Callback<FileInfo, BufferWriter<char>>((fileInfo, buffer) =>
             {
                 callCount++;
                 if (callCount % 2 == 0)
                 {
                     f1.ReadFirstLine(fileInfo, buffer);
                 }
                 else
                 {
                     f2.ReadFirstLine(fileInfo, buffer);
                 }
             })
             .Verifiable();

        var p = new LinuxUtilizationParserCgroupV2(fs.Object, new FakeUserHz(100));

        Task[] tasks = new Task[1_000];
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(p.GetHostCpuUsageInNanoseconds);
        }

        await Task.WhenAll(tasks);

        Assert.True(true);
    }
}

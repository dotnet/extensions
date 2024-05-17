// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Test;

internal sealed class TestResources : IDisposable
{
    public static readonly string TestFilesLocation = "fixtures";

    private static readonly Dictionary<string, string> _files = new(StringComparer.OrdinalIgnoreCase)
    {
        { "/sys/fs/cgroup/cpu/cpu.shares", ""},
        { "/sys/fs/cgroup/memory/memory.limit_in_bytes", "1024"},
        { "/sys/fs/cgroup/memory/memory.max", "1024"},
        { "/sys/fs/cgroup/cpu/cpu.cfs_quota_us", "1"},
        { "/sys/fs/cgroup/cpu/cpu.cfs_period_us", "1" },
        { "/sys/fs/cgroup/cpu.max", "1"},
        { "/proc/meminfo", "MemTotal:       1 kB\r\n"},
        { "/sys/fs/cgroup/cpuset.cpus.effective", "0-3"},
        { "/sys/fs/cgroup/cpu/cpu.wight", "512"},
        { "/sys/fs/cgroup/system.slice/memory.current", "dasda!@#"}
    };

    private static readonly string[] _namesOfDirectories =
    {
        "/sys/fs/cgroup",
        "/sys/fs/cgroup/memory",
        "/sys/fs/cgroup/cpu",
        "/sys/fs/cgroup/system.slice",
        "/proc"
    };

    private readonly HashSet<string> _set = [];
    public readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public void Dispose()
    {
        TearDown();
    }

    public void SetUp()
    {
        foreach (var directoryName in _namesOfDirectories)
        {
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
        }

        foreach (var files in _files)
        {
            if (!File.Exists(files.Key))
            {
                if (string.IsNullOrEmpty(files.Value))
                {
                    File.Create(files.Key).Close();
                    _set.Add(files.Key);
                }
                else
                {
                    using var sw = File.CreateText(files.Key);
                    sw.Write(files.Value);
                    sw.Close();
                    _set.Add(files.Key);
                }
            }
        }
    }

    public void TearDown()
    {
        foreach (var d in _set)
        {
            if (File.Exists(d))
            {
                File.Delete(d);
            }
        }
    }
}

using System.IO;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DependencyInjection;

/// <remarks>
/// Detects cgroup version on Linux OS.
/// </remarks>
public static class ResourceMonitoringLinuxCgroupVersion
{
    /// <remarks>
    /// Get drive metadata for each drive in the system and detects the cgroup version.
    /// </remarks>
    public static bool GetCgroupType()
    {
        DriveInfo[] allDrives = DriveInfo.GetDrives();
        var injectParserV2 = false;
        const string CgroupVersion = "cgroup2fs";
        const string UnifiedCgroupPath = "/sys/fs/cgroup/unified";

        // We check which cgroup version is mounted in the system and based on that we inject the parser.
        foreach (DriveInfo d in allDrives)
        {
            // Currently there are some OS'es which mount both cgroup v1 and v2. And v2 is mounted under /sys/fs/cgroup/unified
            // So, we are checking for the unified cgroup and fallback to cgroup v1, because the path for cgroup v2 is different.
            // This is mostly to support WSL/WSL2, where both cgroup v1 and v2 are mounted and make debugging for Linux easier in VS.
            // https://systemd.io/CGROUP_DELEGATION/#three-different-tree-setups
            if (d.DriveType == DriveType.Ram && d.DriveFormat == CgroupVersion && d.VolumeLabel == UnifiedCgroupPath)
            {
                injectParserV2 = false;
                break;
            }

            if (d.DriveType == DriveType.Ram && d.DriveFormat == CgroupVersion)
            {
                injectParserV2 = true;
                break;
            }
        }

        return injectParserV2;
    }
}

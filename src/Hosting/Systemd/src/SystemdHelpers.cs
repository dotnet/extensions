// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Extensions.Hosting.Systemd
{
    /// <summary>
    /// Helper methods for systemd Services.
    /// </summary>
    public static class SystemdHelpers
    {
        private static bool? _isSystemdService;

        /// <summary>
        /// Check if the current process is hosted as a systemd Service.
        /// </summary>
        /// <returns><c>True</c> if the current process is hosted as a systemd Service, otherwise <c>false</c>.</returns>
        public static bool IsSystemdService()
            => _isSystemdService ?? (bool)(_isSystemdService = CheckParentIsSystemd());

        private static bool CheckParentIsSystemd()
        {
            // No point in testing anything unless it's Unix
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                return false;
            }

            try
            {
                // Check whether our direct parent is 'systemd'.
                var parentPid = GetParentPid();
                var ppidString = parentPid.ToString(NumberFormatInfo.InvariantInfo);

                // If parent PID is not 1, this may be a user unit, in this case it must match MANAGERPID envvar
                if (parentPid != 1
                    && Environment.GetEnvironmentVariable("MANAGERPID") != ppidString)
                {
                    return false;
                }

                // Check parent process name to match "systemd\n"
                var comm = File.ReadAllBytes("/proc/" + ppidString + "/comm");
                return comm.AsSpan().SequenceEqual(Encoding.ASCII.GetBytes("systemd\n"));
            }
            catch
            {
            }

            return false;
        }

        [DllImport("libc", EntryPoint = "getppid")]
        private static extern int GetParentPid();
    }
}

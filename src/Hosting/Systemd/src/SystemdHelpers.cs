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
        private const string INVOCATION_ID = "INVOCATION_ID";

        private static bool? _isSystemdService;

        /// <summary>
        /// Check if the current process is hosted as a systemd Service.
        /// </summary>
        /// <returns><c>True</c> if the current process is hosted as a systemd Service, otherwise <c>false</c>.</returns>
        public static bool IsSystemdService()
            => _isSystemdService ?? (bool)(_isSystemdService = CheckSystemdUnit());

        private static bool CheckSystemdUnit()
        {
            // No point in testing anything unless it's Unix
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                return false;
            }

            // We've got invocation id, it's systemd >= 232 running a unit (either directly or through a child process)
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(INVOCATION_ID)))
            {
                return true;
            }

            // Either it's not a unit, or systemd is < 232, do a bit more digging
            try
            {
                // Test parent process (this matches only direct parents, walking all the way up to the PID 1 is probably not what we would want)
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

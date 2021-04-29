// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Hosting
{
    public class UseSystemdTests
    {
        private readonly ITestOutputHelper _testOutput;
        private const string INVOCATION_ID = "INVOCATION_ID";

        public UseSystemdTests(ITestOutputHelper output)
        {
            _testOutput = output;
        }

        [Fact]
        public void DefaultsToOffOutsideOfService()
        {
            CheckSystemdUnit(_testOutput);

            var host = new HostBuilder()
                .UseSystemd()
                .Build();

            using (host)
            {
                var lifetime = host.Services.GetRequiredService<IHostLifetime>();
                Assert.IsType<ConsoleLifetime>(lifetime);
            }
        }


        private static bool CheckSystemdUnit(ITestOutputHelper output)
        {
            // No point in testing anything unless it's Unix
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                output.WriteLine("Platform not unix");
                return false;
            }

            // We've got invocation id, it's systemd >= 232 running a unit (either directly or through a child process)
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(INVOCATION_ID)))
            {
                output.WriteLine("Invocation ID obtained: " + Environment.GetEnvironmentVariable(INVOCATION_ID));
                return true;
            }

            // Either it's not a unit, or systemd is < 232, do a bit more digging
            try
            {
                // Test parent process (this matches only direct parents, walking all the way up to the PID 1 is probably not what we would want)
                var parentPid = GetParentPid();
                output.WriteLine("ParentPid: " + parentPid);
                var ppidString = parentPid.ToString(NumberFormatInfo.InvariantInfo);
                output.WriteLine("PPidString: " + ppidString);

                // If parent PID is not 1, this may be a user unit, in this case it must match MANAGERPID envvar
                if (parentPid != 1
                    && Environment.GetEnvironmentVariable("MANAGERPID") != ppidString)
                {
                    output.WriteLine("User unit with managerpid: " + Environment.GetEnvironmentVariable("MANAGERPID"));
                    return false;
                }

                // Check parent process name to match "systemd\n"
                var comm = File.ReadAllBytes("/proc/" + ppidString + "/comm");
                output.WriteLine("comm: " + Encoding.ASCII.GetString(comm));
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

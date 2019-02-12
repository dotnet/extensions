// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Microsoft.Extensions.Hosting
{
    public static class ServiceBaseLifetimeHostBuilderExtensions
    {
        /// <summary>
        /// Sets the host lifetime to ServiceBaseLifetime.
        /// </summary>
        /// <remarks>
        /// This is context aware and will only activate under the following circumstances:
        /// - Opted in via the config setting service=true, overrides detection
        /// - Not opted out via the config setting service=false, overrides detection
        /// - Not running in Development
        /// - Running on Windows
        /// - The debugger is not attached
        /// </remarks>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseServiceBaseLifetime(this IHostBuilder hostBuilder)
        {
            if (IsWindowsService())
            {
                // CurrentDirectory for services is c:\Windows\System32, but that's what Host.CreateDefaultBuilder uses for VS scenarios.
                hostBuilder.UseContentRoot(AppContext.BaseDirectory);
                return hostBuilder.ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton<IHostLifetime, ServiceBaseLifetime>();
                });
            }

            return hostBuilder;
        }

        private static bool IsWindowsService()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            var parrent = WindowsServices.Internal.Win32.GetParrentProcess();
            if (parrent == null)
            {
                return false;
            }
            return parrent.SessionId == 0 && "services".Equals(parrent.ProcessName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

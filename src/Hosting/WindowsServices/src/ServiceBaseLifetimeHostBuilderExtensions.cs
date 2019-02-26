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
        /// Sets the host lifetime to ServiceBaseLifetime and sets the Content Root.
        /// </summary>
        /// <remarks>
        /// This is context aware and will only activate if it detects the process is running
        /// as a Windows Service.
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

            var parent = WindowsServices.Internal.Win32.GetParentProcess();
            if (parent == null)
            {
                return false;
            }
            return parent.SessionId == 0 && string.Equals("services", parent.ProcessName, StringComparison.OrdinalIgnoreCase);
        }
    }
}

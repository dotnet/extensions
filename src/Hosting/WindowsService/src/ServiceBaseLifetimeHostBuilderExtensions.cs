// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.WindowsService;

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
            return hostBuilder.ConfigureServices((hostContext, services) =>
            {
                bool? isService = null;
                var config = hostContext.Configuration;

                // Opt out
                if (string.Equals(config["service"], "false", StringComparison.OrdinalIgnoreCase))
                {
                    isService = false;
                }
                // Opt in
                else if (string.Equals(config["service"], "true", StringComparison.OrdinalIgnoreCase))
                {
                    isService = true;
                }
                // Guess?
                else if (!hostContext.HostingEnvironment.IsDevelopment()
                    && RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    && !Debugger.IsAttached)
                {
                    isService = true;
                }

                if (isService == true)
                {
                    services.AddSingleton<IHostLifetime, ServiceBaseLifetime>();
                }
            });
        }
    }
}

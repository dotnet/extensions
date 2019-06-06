// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.SystemdServices;
using Microsoft.Extensions.Logging.Console;

namespace Microsoft.Extensions.Hosting
{
    /// <summary>
    /// Extension methods for setting up SystemdServiceLifetime.
    /// </summary>
    public static class SystemdServiceHostBuilderExtensions
    {
        /// <summary>
        /// Sets the host lifetime to SystemdServiceLifetime,
        /// provides notification messages for application started and stopping,
        /// and configures console logging to the systemd format.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     This is context aware and will only activate if it detects the process is running
        ///     as a systemd Service.
        ///   </para>
        ///   <para>
        ///     The systemd service file must be configured with <c>Type=notify</c> to enable
        ///     notifications. See https://www.freedesktop.org/software/systemd/man/systemd.service.html.
        ///   </para>
        /// </remarks>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static IHostBuilder UseSystemdService(this IHostBuilder hostBuilder)
        {
            if (ServiceHelpers.IsSystemdService())
            {
                hostBuilder.ConfigureServices((hostContext, services) =>
                {
                    services.Configure<ConsoleLoggerOptions>(options =>
                    {
                        options.Format = ConsoleLoggerFormat.Systemd;
                    });

                    services.AddSingleton<ISystemdNotifier, SystemdNotifier>();

                    services.AddSingleton<IHostLifetime, SystemdServiceLifetime>();
                });
            }
            return hostBuilder;
        }
    }
}
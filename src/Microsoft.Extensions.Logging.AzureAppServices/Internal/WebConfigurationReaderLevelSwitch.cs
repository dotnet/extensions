// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Serilog.Core;
using Serilog.Events;

namespace Microsoft.Extensions.Logging.AzureAppServices.Internal
{
    /// <summary>
    /// The <see cref="LoggingLevelSwitch"/> implementation that runs callback
    /// when <see cref="IWebAppLogConfigurationReader.OnConfigurationChanged"/> event fires.
    /// </summary>
    public class WebConfigurationReaderLevelSwitch : LoggingLevelSwitch
    {
        /// <summary>
        /// The log level at which the logger is disabled.
        /// </summary>
        private static readonly LogEventLevel LogLevelDisabled = LogEventLevel.Fatal + 1;

        /// <summary>
        /// Creates a new instance of the <see cref="WebConfigurationReaderLevelSwitch"/> class.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="convert"></param>
        public WebConfigurationReaderLevelSwitch(IWebAppLogConfigurationReader reader, Func<WebAppLogConfiguration, LogLevel> convert )
        {
            reader.OnConfigurationChanged += (sender, configuration) =>
            {
                MinimumLevel = LogLevelToLogEventLevel(convert(configuration));
            };
        }

        /// <summary>
        /// Converts a <see cref="LogLevel"/> object to <see cref="LogEventLevel"/>.
        /// </summary>
        /// <param name="logLevel">The log level to convert</param>
        /// <returns>A <see cref="LogEventLevel"/> instance</returns>
        private static LogEventLevel LogLevelToLogEventLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return LogEventLevel.Verbose;

                case LogLevel.Debug:
                    return LogEventLevel.Debug;

                case LogLevel.Information:
                    return LogEventLevel.Information;

                case LogLevel.Warning:
                    return LogEventLevel.Warning;

                case LogLevel.Error:
                    return LogEventLevel.Error;

                case LogLevel.Critical:
                    return LogEventLevel.Fatal;

                case LogLevel.None:
                    return LogLevelDisabled;

                default:
                    throw new ArgumentOutOfRangeException($"Unknown log level: {logLevel}");
            }
        }
    }
}
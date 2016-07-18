// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal
{
    /// <summary>
    /// Represents a Serilog logger provider use for Azure WebApp.
    /// </summary>
    public abstract class SerilogLoggerProvider : ILoggerProvider
    {
        // Solution suggested by the Serilog creator http://stackoverflow.com/questions/30849166/how-to-turn-off-serilog
        /// <summary>
        /// The log level at which the logger is disabled.
        /// </summary>
        protected static LogEventLevel LogLevelDisabled = ((LogEventLevel)1 + (int)LogEventLevel.Fatal);

        private readonly LoggingLevelSwitch _levelSwitch = new LoggingLevelSwitch();

        private readonly IWebAppLogConfigurationReader _configReader;
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Creates a new instance of the <see cref="SerilogLoggerProvider"/> class.
        /// </summary>
        /// <param name="configReader">The configuration reader</param>
        /// <param name="configureLogger">The actions required to configure the logger</param>
        public SerilogLoggerProvider(IWebAppLogConfigurationReader configReader, Action<LoggerConfiguration, WebAppLogConfiguration> configureLogger)
        {
            if (configReader == null)
            {
                throw new ArgumentNullException(nameof(configReader));
            }
            if (configureLogger == null)
            {
                throw new ArgumentNullException(nameof(configureLogger));
            }

            _configReader = configReader;
            var webAppsConfiguration = configReader.Current;

            configReader.OnConfigurationChanged += OnConfigurationChanged;

            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.ControlledBy(_levelSwitch);
            configureLogger(loggerConfiguration, webAppsConfiguration);
            var serilogLogger = loggerConfiguration.CreateLogger();
            
            OnConfigurationChanged(webAppsConfiguration);

            _loggerFactory = new LoggerFactory();
            _loggerFactory.AddSerilog(serilogLogger);
        }

        /// <summary>
        /// The switch used the modify the logging level.
        /// </summary>
        protected LoggingLevelSwitch LevelSwitcher => _levelSwitch;

        /// <summary>
        /// Called when the configuration changes
        /// </summary>
        /// <param name="newConfiguration">The new configuration values</param>
        protected abstract void OnConfigurationChanged(WebAppLogConfiguration newConfiguration);

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public void Dispose()
        {
            _configReader.OnConfigurationChanged -= OnConfigurationChanged;
            _loggerFactory.Dispose();
        }

        private void OnConfigurationChanged(object sender, WebAppLogConfiguration newConfiguration)
        {
            OnConfigurationChanged(newConfiguration);
        }

        /// <summary>
        /// Converts a <see cref="LogLevel"/> object to <see cref="LogEventLevel"/>.
        /// </summary>
        /// <param name="logLevel">The log level to convert</param>
        /// <returns>A <see cref="LogEventLevel"/> instance</returns>
        protected static LogEventLevel LogLevelToLogEventLevel(LogLevel logLevel)
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

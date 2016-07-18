// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Serilog;
using Serilog.Formatting.Display;
using Serilog.Sinks.RollingFile;

namespace Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal
{
    /// <summary>
    /// A file logger for Azure WebApp.
    /// </summary>
    public class FileLoggerProvider : SerilogLoggerProvider
    {
        /// <summary>
        /// The default file size limit in megabytes
        /// </summary>
        public const int DefaultFileSizeLimitMb = 10;

        // Two days retention limit is okay because the file logger turns itself off after 12 hours (portal feature)
        private const int RetainedFileCountLimit = 2; // Days (also number of files because we have 1 file/day)

        private const string OutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";
        private const string FileNamePattern = "diagnostics-{Date}.txt";

        /// <summary>
        /// Creates a new instance of the <see cref="FileLoggerProvider"/> class.
        /// </summary>
        /// <param name="configReader">A configuration reader</param>
        /// <param name="fileSizeLimit">A strictly positive value representing the maximum log size in megabytes. Once the log is full, no more message will be appended</param>
        public FileLoggerProvider(IWebAppLogConfigurationReader configReader, int fileSizeLimit)
            : base(configReader, (loggerConfiguration, webAppConfiguration) =>
            {
                if (string.IsNullOrEmpty(webAppConfiguration.FileLoggingFolder))
                {
                    throw new ArgumentNullException(nameof(webAppConfiguration.FileLoggingFolder), "The file logger path cannot be null or empty.");
                }

                var logsFolder = webAppConfiguration.FileLoggingFolder;
                if (!Directory.Exists(logsFolder))
                {
                    Directory.CreateDirectory(logsFolder);
                }
                var logsFilePattern = Path.Combine(logsFolder, FileNamePattern);

                var fileSizeLimitBytes = fileSizeLimit * 1024 * 1024;

                var messageFormatter = new MessageTemplateTextFormatter(OutputTemplate, null);
                var rollingFileSink = new RollingFileSink(logsFilePattern, messageFormatter, fileSizeLimitBytes, RetainedFileCountLimit);
                var backgroundSink = new BackgroundSink(rollingFileSink, BackgroundSink.DefaultLogMessagesQueueSize);

                loggerConfiguration.WriteTo.Sink(backgroundSink);
            })
        {
        }

        /// <inheritdoc />
        protected override void OnConfigurationChanged(WebAppLogConfiguration newConfiguration)
        {
            if (!newConfiguration.FileLoggingEnabled)
            {
                LevelSwitcher.MinimumLevel = LogLevelDisabled;
            }
            else
            {
                LevelSwitcher.MinimumLevel = LogLevelToLogEventLevel(newConfiguration.FileLoggingLevel);
            }
        }
    }
}

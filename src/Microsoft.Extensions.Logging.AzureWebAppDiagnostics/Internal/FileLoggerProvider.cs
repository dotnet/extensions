// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Display;
using Serilog.Sinks.RollingFile;

namespace Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal
{
    /// <summary>
    /// The logger provider that creates instances of <see cref="Serilog.Core.Logger"/>.
    /// </summary>
    public class FileLoggerProvider
    {
        private readonly int _fileSizeLimit;
        private readonly int _retainedFileCountLimit;
        private readonly int _backgroundQueueSize;
        private readonly string _outputTemplate;

        private const string FileNamePattern = "diagnostics-{Date}.txt";

        /// <summary>
        /// Creates a new instance of the <see cref="FileLoggerProvider"/> class.
        /// </summary>
        /// <param name="fileSizeLimit">A strictly positive value representing the maximum log size in megabytes. Once the log is full, no more message will be appended</param>
        /// <param name="retainedFileCountLimit">A strictly positive value representing the maximum retained file count</param>
        /// <param name="backgroundQueueSize">The maximum size of the background queue</param>
        /// <param name="outputTemplate">A message template describing the output messages</param>
        public FileLoggerProvider(int fileSizeLimit, int retainedFileCountLimit, int backgroundQueueSize, string outputTemplate)
        {
            if (outputTemplate == null)
            {
                throw new ArgumentNullException(nameof(outputTemplate));
            }
            if (fileSizeLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fileSizeLimit), $"{nameof(fileSizeLimit)} should be positive.");
            }
            if (retainedFileCountLimit <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retainedFileCountLimit), $"{nameof(retainedFileCountLimit)} should be positive.");
            }

            _fileSizeLimit = fileSizeLimit;
            _retainedFileCountLimit = retainedFileCountLimit;
            _backgroundQueueSize = backgroundQueueSize;
            _outputTemplate = outputTemplate;
        }

        /// <inheritdoc />
        public Logger ConfigureLogger(IWebAppLogConfigurationReader reader)
        {
            var webAppConfiguration = reader.Current;
            if (string.IsNullOrEmpty(webAppConfiguration.FileLoggingFolder))
            {
                throw new ArgumentNullException(nameof(webAppConfiguration.FileLoggingFolder),
                    "The file logger path cannot be null or empty.");
            }

            var logsFolder = webAppConfiguration.FileLoggingFolder;
            if (!Directory.Exists(logsFolder))
            {
                Directory.CreateDirectory(logsFolder);
            }
            var logsFilePattern = Path.Combine(logsFolder, FileNamePattern);

            var messageFormatter = new MessageTemplateTextFormatter(_outputTemplate, null);
            var rollingFileSink = new RollingFileSink(logsFilePattern, messageFormatter, _fileSizeLimit, _retainedFileCountLimit);
            var backgroundSink = new BackgroundSink(rollingFileSink, _backgroundQueueSize);

            var loggerConfiguration = new LoggerConfiguration();
            loggerConfiguration.WriteTo.Sink(backgroundSink);
            loggerConfiguration.MinimumLevel.ControlledBy(new WebConfigurationReaderLevelSwitch(reader,
                configuration =>
                {
                    return configuration.FileLoggingEnabled ? configuration.FileLoggingLevel : LogLevel.None;
                }));
            return loggerConfiguration.CreateLogger();
        }
    }
}

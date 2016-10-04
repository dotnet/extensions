// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.WindowsAzure.Storage.Blob;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Display;

namespace Microsoft.Extensions.Logging.AzureAppServices.Internal
{
    /// <summary>
    /// The implemenation of logger provider that creates instances of <see cref="Serilog.Core.Logger"/>.
    /// </summary>
    public class AzureBlobLoggerProvider
    {
        private readonly string _outputTemplate;
        private readonly string _appName;
        private readonly string _instanceId;
        private readonly string _fileName;
        private readonly int _batchSize;
        private readonly int _backgroundQueueSize;
        private readonly TimeSpan _period;

        /// <summary>
        /// Creates a new instance of the <see cref="AzureBlobLoggerProvider"/> class.
        /// </summary>
        /// <param name="outputTemplate">A message template describing the output messages</param>
        /// <param name="appName">The application name to use in blob name</param>
        /// <param name="instanceId">The application instance id to use in blob name</param>
        /// <param name="fileName">The last section in log blob name</param>
        /// <param name="batchSize">A maximum number of events to include in a single blob append batch</param>
        /// <param name="backgroundQueueSize">The maximum size of the background queue</param>
        /// <param name="period">A time to wait between checking for blob log batches</param>
        public AzureBlobLoggerProvider(string outputTemplate, string appName, string instanceId, string fileName, int batchSize, int backgroundQueueSize, TimeSpan period)
        {
            if (outputTemplate == null)
            {
                throw new ArgumentNullException(nameof(outputTemplate));
            }
            if (appName == null)
            {
                throw new ArgumentNullException(nameof(appName));
            }
            if (instanceId == null)
            {
                throw new ArgumentNullException(nameof(instanceId));
            }
            if (fileName == null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }
            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), $"{nameof(batchSize)} should be a positive number.");
            }
            if (period <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(period), $"{nameof(period)} should be longer than zero.");
            }

            _outputTemplate = outputTemplate;
            _appName = appName;
            _instanceId = instanceId;
            _fileName = fileName;
            _batchSize = batchSize;
            _backgroundQueueSize = backgroundQueueSize;
            _period = period;
        }

        /// <inheritdoc />
        public Logger ConfigureLogger(IWebAppLogConfigurationReader reader)
        {
            var messageFormatter = new MessageTemplateTextFormatter(_outputTemplate, null);
            var container = new CloudBlobContainer(new Uri(reader.Current.BlobContainerUrl));
            var fileName = _instanceId + "-" + _fileName;
            var azureBlobSink = new AzureBlobSink(
                name => new BlobAppendReferenceWrapper(container.GetAppendBlobReference(name)),
                _appName,
                fileName,
                messageFormatter,
                _batchSize,
                _period);

            var backgroundSink = new BackgroundSink(azureBlobSink, _backgroundQueueSize);
            var loggerConfiguration = new LoggerConfiguration();

            loggerConfiguration.WriteTo.Sink(backgroundSink);
            loggerConfiguration.MinimumLevel.ControlledBy(new WebConfigurationReaderLevelSwitch(reader,
                configuration =>
                {
                    return configuration.BlobLoggingEnabled ? configuration.BlobLoggingLevel : LogLevel.None;
                }));

            return loggerConfiguration.CreateLogger();
        }

    }
}
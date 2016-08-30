// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal;
using Serilog;

namespace Microsoft.Extensions.Logging.AzureWebAppDiagnostics
{
    /// <summary>
    /// Logger provider for Azure WebApp.
    /// </summary>
    public class AzureWebAppDiagnosticsLoggerProvider : ILoggerProvider
    {
        private readonly IWebAppLogConfigurationReader _configurationReader;

        private readonly LoggerFactory _loggerFactory;

        /// <summary>
        /// Creates a new instance of the <see cref="AzureWebAppDiagnosticsLoggerProvider"/> class.
        /// </summary>
        public AzureWebAppDiagnosticsLoggerProvider(WebAppContext context, AzureWebAppDiagnosticsSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _configurationReader  = new WebAppLogConfigurationReader(context);

            var config = _configurationReader.Current;
            var runningInWebApp = config.IsRunningInWebApp;

            if (runningInWebApp)
            {
                _loggerFactory = new LoggerFactory();
                var fileLoggerProvider = new FileLoggerProvider(
                    settings.FileSizeLimit,
                    settings.RetainedFileCountLimit,
                    settings.BackgroundQueueSize,
                    settings.OutputTemplate);

                _loggerFactory.AddSerilog(fileLoggerProvider.ConfigureLogger(_configurationReader));
                if (!string.IsNullOrEmpty(config.BlobContainerUrl))
                {
                    var blobLoggerProvider = new AzureBlobLoggerProvider(
                        settings.OutputTemplate,
                        context.SiteName,
                        context.SiteInstanceId,
                        settings.BlobName,
                        settings.BlobBatchSize,
                        settings.BackgroundQueueSize,
                        settings.BlobCommitPeriod);
                    _loggerFactory.AddSerilog(blobLoggerProvider.ConfigureLogger(_configurationReader));
                }
            }
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory?.CreateLogger(categoryName) ?? NullLogger.Instance;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _loggerFactory?.Dispose();
            _configurationReader.Dispose();
        }
    }
}

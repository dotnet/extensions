// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions.Internal;
using Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal;

namespace Microsoft.Extensions.Logging.AzureWebAppDiagnostics
{
    /// <summary>
    /// Logger provider for Azure WebApp.
    /// </summary>
    public class AzureWebAppDiagnosticsLoggerProvider : ILoggerProvider
    {
        private readonly IWebAppLogConfigurationReader _configurationReader;

        private readonly ILoggerProvider _innerLoggerProvider;
        private readonly bool _runningInWebApp;

        /// <summary>
        /// Creates a new instance of the <see cref="AzureWebAppDiagnosticsLoggerProvider"/> class.
        /// </summary>
        public AzureWebAppDiagnosticsLoggerProvider(WebAppContext context, int fileSizeLimitMb)
        {
            _configurationReader  = new WebAppLogConfigurationReader(context);

            var config = _configurationReader.Current;
            _runningInWebApp = config.IsRunningInWebApp;

            if (!_runningInWebApp)
            {
                _innerLoggerProvider = NullLoggerProvider.Instance;
            }
            else
            {
                _innerLoggerProvider = new FileLoggerProvider(_configurationReader, fileSizeLimitMb);

                if (!string.IsNullOrEmpty(config.BlobContainerUrl))
                {
                    // TODO: Add the blob logger by creating a composite inner logger which calls
                    // both loggers
                }
            }
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName)
        {
            return _innerLoggerProvider.CreateLogger(categoryName);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _innerLoggerProvider.Dispose();
            _configurationReader.Dispose();
        }
    }
}

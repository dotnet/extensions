// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Logging.Azure.AppServicesDiagnostics.Internal
{
    /// <summary>
    /// Used to create instances of <see cref="WebAppLogConfiguration"/>
    /// </summary>
    public class WebAppLogConfigurationBuilder
    {
        private bool _isRunningInAzureWebApps;

        private bool _fileLoggingEnabled;
        private LogLevel _fileLoggingLevel = LogLevel.None;
        private string _fileLoggingFolder;

        private bool _blobLoggingEnabled;
        private LogLevel _blobLoggingLevel = LogLevel.None;
        private string _blobContainerUrl;

        /// <summary>
        /// Sets a value indicating whether or not we're in an Azure context
        /// </summary>
        /// <param name="isRunningInAzureWebApps">True if running in Azure, false otherwise</param>
        /// <returns>The builder instance</returns>
        public WebAppLogConfigurationBuilder SetIsRunningInAzureWebApps(bool isRunningInAzureWebApps)
        {
            _isRunningInAzureWebApps = isRunningInAzureWebApps;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether or not file logging is enabled
        /// </summary>
        /// <param name="fileLoggingEnabled">True if file logging is enabled, false otherwise</param>
        /// <returns>The builder instance</returns>
        public WebAppLogConfigurationBuilder SetFileLoggingEnabled(bool fileLoggingEnabled)
        {
            _fileLoggingEnabled = fileLoggingEnabled;
            return this;
        }

        /// <summary>
        /// Sets logging level for the file logger
        /// </summary>
        /// <param name="logLevel">File logging level</param>
        /// <returns>The builder instance</returns>
        public WebAppLogConfigurationBuilder SetFileLoggingLevel(LogLevel logLevel)
        {
            _fileLoggingLevel = logLevel;
            return this;
        }

        /// <summary>
        /// Sets the folder in which file logs end up
        /// </summary>
        /// <param name="folder">File logging folder</param>
        /// <returns>The builder instance</returns>
        public WebAppLogConfigurationBuilder SetFileLoggingFolder(string folder)
        {
            _fileLoggingFolder = folder;
            return this;
        }

        /// <summary>
        /// Sets a value indicating whether or not blob logging is enabled
        /// </summary>
        /// <param name="blobLoggingEnabled">True if file logging is enabled, false otherwise</param>
        /// <returns>The builder instance</returns>
        public WebAppLogConfigurationBuilder SetBlobLoggingEnabled(bool blobLoggingEnabled)
        {
            _blobLoggingEnabled = blobLoggingEnabled;
            return this;
        }

        /// <summary>
        /// Sets logging level for the blob logger
        /// </summary>
        /// <param name="logLevel">Blob logging level</param>
        /// <returns>The builder instance</returns>
        public WebAppLogConfigurationBuilder SetBlobLoggingLevel(LogLevel logLevel)
        {
            _blobLoggingLevel = logLevel;
            return this;
        }

        /// <summary>
        /// Sets blob logging url
        /// </summary>
        /// <param name="blobUrl">The container in which blobs are placed</param>
        /// <returns>The builder instance</returns>
        public WebAppLogConfigurationBuilder SetBlobLoggingUrl(string blobUrl)
        {
            _blobContainerUrl = blobUrl;
            return this;
        }

        /// <summary>
        /// Builds the <see cref="WebAppLogConfiguration"/> instance
        /// </summary>
        /// <returns>The configuration object</returns>
        public WebAppLogConfiguration Build()
        {
            return new WebAppLogConfiguration(
                isRunningInWebApp: _isRunningInAzureWebApps,
                fileLoggingEnabled: _fileLoggingEnabled,
                fileLoggingLevel: _fileLoggingLevel,
                fileLoggingFolder: _fileLoggingFolder,
                blobLoggingEnabled: _blobLoggingEnabled,
                blobLoggingLevel: _blobLoggingLevel,
                blobContainerUrl: _blobContainerUrl);
        }
    }
}

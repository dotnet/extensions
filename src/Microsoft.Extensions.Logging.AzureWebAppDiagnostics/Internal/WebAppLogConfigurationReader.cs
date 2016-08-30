// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Logging.AzureWebAppDiagnostics.Internal
{
    /// <summary>
    /// Represents the default implementation of the <see cref="IWebAppLogConfigurationReader"/>.
    /// </summary>
    public class WebAppLogConfigurationReader : IWebAppLogConfigurationReader
    {
        private readonly IConfigurationRoot _configuration;
        private readonly string _fileLogFolder;

        private WebAppLogConfiguration _latestConfiguration;
        private IDisposable _changeSubscription;

        /// <inheritdoc />
        public event EventHandler<WebAppLogConfiguration> OnConfigurationChanged;

        /// <summary>
        /// Creates a new instance of the <see cref="WebAppLogConfigurationReader"/> class.
        /// </summary>
        /// <param name="context">The context in which the reader runs</param>
        public WebAppLogConfigurationReader(IWebAppContext context)
        {
            if (!context.IsRunningInAzureWebApp)
            {
                _latestConfiguration = WebAppLogConfiguration.Disabled;
            }
            else
            {
                _fileLogFolder = Path.Combine(context.HomeFolder, "LogFiles", "Application");
                var settingsFolder = Path.Combine(context.HomeFolder, "site", "diagnostics");
                var settingsFile = Path.Combine(settingsFolder, "settings.json");

                // TODO: This is a workaround because the file provider doesn't handle missing folders/files
                if (!Directory.Exists(settingsFolder))
                {
                    Directory.CreateDirectory(settingsFolder);
                }
                if (!File.Exists(settingsFile))
                {
                    File.WriteAllText(settingsFile, "{}");
                }

                _configuration = new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .AddJsonFile(settingsFile, optional: true, reloadOnChange: true)
                    .Build();

                SubscribeToConfigurationChangeEvent();
                ReloadConfiguration();
            }
        }

        /// <inheritdoc />
        public WebAppLogConfiguration Current
        {
            get
            {
                return _latestConfiguration;
            }
        }

        /// <summary>
        /// Disposes the object instance.
        /// </summary>
        public void Dispose()
        {
            DisposeChangeSubscription();
        }

        private void OnConfigurationTokenChange(object state)
        {
            ReloadConfiguration();
            SubscribeToConfigurationChangeEvent();

            OnConfigurationChanged?.Invoke(this, _latestConfiguration);
        }

        private void SubscribeToConfigurationChangeEvent()
        {
            DisposeChangeSubscription();

            // The token from configuration has to be renewed after each trigger
            var changeToken = _configuration.GetReloadToken();
            _changeSubscription = changeToken.RegisterChangeCallback(OnConfigurationTokenChange, null);
        }

        private void ReloadConfiguration()
        {
            // Don't use the binder because of all the defaults that we want in place
            _latestConfiguration = new WebAppLogConfigurationBuilder()
                .SetIsRunningInAzureWebApps(true)
                .SetFileLoggingEnabled(TextToBoolean(_configuration.GetSection("AzureDriveEnabled")?.Value))
                .SetFileLoggingLevel(TextToLogLevel(_configuration.GetSection("AzureDriveTraceLevel")?.Value))
                .SetFileLoggingFolder(_fileLogFolder)
                .SetBlobLoggingEnabled(TextToBoolean(_configuration.GetSection("AzureBlobEnabled")?.Value))
                .SetBlobLoggingLevel(TextToLogLevel(_configuration.GetSection("AzureBlobTraceLevel")?.Value))
                .SetBlobLoggingUrl(_configuration.GetSection("APPSETTING_DIAGNOSTICS_AZUREBLOBCONTAINERSASURL")?.Value)
                .Build();
        }

        private void DisposeChangeSubscription()
        {
            if (_changeSubscription != null)
            {
                _changeSubscription.Dispose();
                _changeSubscription = null;
            }
        }

        private static bool TextToBoolean(string text)
        {
            bool result;
            if (string.IsNullOrEmpty(text) ||
                !bool.TryParse(text, out result))
            {
                result = false;
            }

            return result;
        }

        private static LogLevel TextToLogLevel(string text)
        {
            switch (text?.ToUpperInvariant())
            {
                case "ERROR":
                    return LogLevel.Error;
                case "WARNING":
                    return LogLevel.Warning;
                case "INFORMATION":
                    return LogLevel.Information;
                case "VERBOSE":
                    return LogLevel.Trace;
                default:
                    return LogLevel.None;
            }
        }
    }
}

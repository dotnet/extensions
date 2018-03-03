// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging.Console
{
    public class ConfigurationConsoleLoggerSettings : IConsoleLoggerSettings
    {
        private readonly IConfiguration _configuration;

        public ConfigurationConsoleLoggerSettings(IConfiguration configuration)
        {
            _configuration = configuration;
            ChangeToken = configuration.GetReloadToken();
        }

        public IChangeToken ChangeToken { get; private set; }

        private bool GetBooleanConfigurationValue(string keyName)
        {
            bool parsedValue;
            string value = _configuration[keyName];
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }
            else if (bool.TryParse(value, out parsedValue))
            {
                return parsedValue;
            }
            else
            {
                var message = $"Configuration value '{value}' for setting '{keyName}' is not supported.";
                throw new InvalidOperationException(message);
            }
        }

        public bool IncludeScopes
        {
            get
            {
                return GetBooleanConfigurationValue(nameof(IncludeScopes));
            }
        }

        public bool DisableColors
        {
            get
            {
                return GetBooleanConfigurationValue(nameof(DisableColors));
            }
        }

        public IConsoleLoggerSettings Reload()
        {
            ChangeToken = null;
            return new ConfigurationConsoleLoggerSettings(_configuration);
        }

        public bool TryGetSwitch(string name, out LogLevel level)
        {
            var switches = _configuration.GetSection("LogLevel");
            if (switches == null)
            {
                level = LogLevel.None;
                return false;
            }

            var value = switches[name];
            if (string.IsNullOrEmpty(value))
            {
                level = LogLevel.None;
                return false;
            }
            else if (Enum.TryParse<LogLevel>(value, true, out level))
            {
                return true;
            }
            else
            {
                var message = $"Configuration value '{value}' for category '{name}' is not supported.";
                throw new InvalidOperationException(message);
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging.AzureEventHubs
{
    [ProviderAlias("AzureEventHubs")]
    public class AzureEventHubsLoggerProvider : ILoggerProvider
    {
        private readonly IOptionsMonitor<AzureEventHubsLoggerOptions> _options;
        private readonly ConcurrentDictionary<string, AzureEventHubsLogger> _loggers;
        private readonly IAzureEventHubsLoggerFormatter _formatter;
        private readonly IAzureEventHubsLoggerProcessor _processor;

        private IDisposable _optionsReloadToken;
        private IExternalScopeProvider _scopeProvider = NullExternalScopeProvider.Instance;

        public AzureEventHubsLoggerProvider(IOptionsMonitor<AzureEventHubsLoggerOptions> options, IAzureEventHubsLoggerFormatter formatter, IAzureEventHubsLoggerProcessor processor)
        {
            _options = options;
            _loggers = new ConcurrentDictionary<string, AzureEventHubsLogger>();

            ReloadLoggerOptions(options.CurrentValue);

            _formatter = formatter;
            _processor = processor;
        }

        private void ReloadLoggerOptions(AzureEventHubsLoggerOptions options)
        {
            foreach (var logger in _loggers)
            {
                logger.Value.Options = options;
            }

            _optionsReloadToken = _options.OnChange(ReloadLoggerOptions);
        }

        /// <inheritdoc />
        public ILogger CreateLogger(string name)
        {
            return _loggers.GetOrAdd(name, _ => new AzureEventHubsLogger(name, _formatter, _processor)
            {
                Options = _options.CurrentValue,
                ScopeProvider = _scopeProvider
            });
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
            _processor.Dispose();
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;

            foreach (var logger in _loggers)
            {
                logger.Value.ScopeProvider = _scopeProvider;
            }
        }
    }
}

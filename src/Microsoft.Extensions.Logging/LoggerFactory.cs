// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    public class LoggerFactory : ILoggerFactory
    {
        private static readonly LoggerRuleSelector RuleSelector = new LoggerRuleSelector();

        private readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>(StringComparer.Ordinal);

        private readonly List<ProviderRegistration> _providerRegistrations;
        private readonly object _sync = new object();
        private volatile bool _disposed;
        private IDisposable _changeTokenRegistration;
        private LoggerFilterOptions _filterOptions;

        public LoggerFactory() : this(Enumerable.Empty<ILoggerProvider>())
        {
        }

        public LoggerFactory(IEnumerable<ILoggerProvider> providers) : this(providers, new StaticFilterOptionsMonitor(new LoggerFilterOptions()))
        {
        }

        public LoggerFactory(IEnumerable<ILoggerProvider> providers, LoggerFilterOptions filterOptions) : this(providers, new StaticFilterOptionsMonitor(filterOptions))
        {
        }

        public LoggerFactory(IEnumerable<ILoggerProvider> providers, IOptionsMonitor<LoggerFilterOptions> filterOption)
        {
            _providerRegistrations = providers.Select(provider => new ProviderRegistration { Provider = provider }).ToList();
            _changeTokenRegistration = filterOption.OnChange(RefreshFilters);
            RefreshFilters(filterOption.CurrentValue);
        }

        private void RefreshFilters(LoggerFilterOptions filterOptions)
        {
            lock (_sync)
            {
                _filterOptions = filterOptions;
                foreach (var logger in _loggers)
                {
                    var loggerInformation = logger.Value.Loggers;
                    var categoryName = logger.Key;

                    ApplyRules(loggerInformation, categoryName, 0, loggerInformation.Length);
                }
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            lock (_sync)
            {
                Logger logger;

                if (!_loggers.TryGetValue(categoryName, out logger))
                {

                    logger = new Logger()
                    {
                        Loggers = CreateLoggers(categoryName)
                    };
                    _loggers[categoryName] = logger;
                }

                return logger;
            }
        }

        public void AddProvider(ILoggerProvider provider)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            lock (_sync)
            {
                _providerRegistrations.Add(new ProviderRegistration { Provider = provider, ShouldDispose = true});
                foreach (var logger in _loggers)
                {
                    var loggerInformation = logger.Value.Loggers;
                    var categoryName = logger.Key;

                    Array.Resize(ref loggerInformation, loggerInformation.Length + 1);
                    var newLoggerIndex = loggerInformation.Length - 1;
                    loggerInformation[newLoggerIndex].Logger = provider.CreateLogger(categoryName);
                    loggerInformation[newLoggerIndex].ProviderType = provider.GetType();

                    ApplyRules(loggerInformation, categoryName, newLoggerIndex, 1);

                    logger.Value.Loggers = loggerInformation;
                }
            }
        }

        private LoggerInformation[] CreateLoggers(string categoryName)
        {
            var loggers = new LoggerInformation[_providerRegistrations.Count];
            for (int i = 0; i < _providerRegistrations.Count; i++)
            {
                var provider = _providerRegistrations[i].Provider;

                loggers[i].Logger = provider.CreateLogger(categoryName);
                loggers[i].ProviderType = provider.GetType();
            }

            ApplyRules(loggers, categoryName, 0, loggers.Length);
            return loggers;
        }

        private void ApplyRules(LoggerInformation[] loggers, string categoryName, int start, int count)
        {
            for (var index = start; index < start + count; index++)
            {
                ref var loggerInformation = ref loggers[index];

                RuleSelector.Select(_filterOptions,
                    loggerInformation.ProviderType,
                    categoryName,
                    out var minLevel,
                    out var filter);

                loggerInformation.Category = categoryName;
                loggerInformation.MinLevel = minLevel;
                loggerInformation.Filter = filter;
            }
        }

        /// <summary>
        /// Check if the factory has been disposed.
        /// </summary>
        /// <returns>True when <see cref="Dispose()"/> as been called</returns>
        protected virtual bool CheckDisposed() => _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _changeTokenRegistration?.Dispose();

                foreach (var registration in _providerRegistrations)
                {
                    try
                    {
                        if (registration.ShouldDispose)
                        {
                            registration.Provider.Dispose();
                        }
                    }
                    catch
                    {
                        // Swallow exceptions on dispose
                    }
                }
            }
        }

        private struct ProviderRegistration
        {
            public ILoggerProvider Provider;
            public bool ShouldDispose;
        }
    }
}
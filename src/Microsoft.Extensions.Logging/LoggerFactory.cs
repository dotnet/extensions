// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Summary description for LoggerFactory
    /// </summary>
    public class LoggerFactory : ILoggerFactory
    {
        private readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>(StringComparer.Ordinal);
        private KeyValuePair<ILoggerProvider, string>[] _providers = new KeyValuePair<ILoggerProvider, string>[0];
        private readonly object _sync = new object();
        private volatile bool _disposed;
        private readonly IConfiguration _configuration;
        private IChangeToken _changeToken;
        private Dictionary<string, LogLevel> _defaultFilter;
        private Func<string, string, LogLevel, bool> _genericFilters;
        private Dictionary<string, Func<string, LogLevel, bool>> _providerFilters = new Dictionary<string, Func<string, LogLevel, bool>>();
        private Dictionary<string, Func<string, LogLevel, bool>> _categoryFilters = new Dictionary<string, Func<string, LogLevel, bool>>();

        private static readonly Func<string, string, LogLevel, bool> _trueFilter = (providerName, category, level) => true;
        private static readonly Func<string, LogLevel, bool> _categoryTrueFilter = (n, l) => true;

        public LoggerFactory()
        {
            _genericFilters = _trueFilter;
        }

        public LoggerFactory(IConfiguration configuration)
            : this()
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _configuration = configuration;
            _changeToken = configuration.GetReloadToken();
            _changeToken.RegisterChangeCallback(OnConfigurationReload, null);

            LoadDefaultConfigValues();
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            Logger logger;
            lock (_sync)
            {
                if (!_loggers.TryGetValue(categoryName, out logger))
                {
                    Func<string, LogLevel, bool> filter = _categoryTrueFilter;
                    foreach (var prefix in GetKeyPrefixes(categoryName))
                    {
                        if (_categoryFilters.TryGetValue(prefix, out var categoryFilter))
                        {
                            var previousFilter = filter;
                            filter = (providerName, level) =>
                            {
                                if (previousFilter(providerName, level))
                                {
                                    return categoryFilter(providerName, level);
                                }

                                return false;
                            };
                        }
                    }
                    logger = new Logger(this, categoryName, filter);
                    _loggers[categoryName] = logger;
                }
            }

            return logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            // REVIEW: Should we do the name resolution for our providers like this?
            var name = string.Empty;
            switch (provider.GetType().FullName)
            {
                case "Microsoft.Extensions.Logging.ConsoleLoggerProvider":
                    name = "Console";
                    break;
                case "Microsoft.Extensions.Logging.DebugLoggerProvider":
                    name = "Debug";
                    break;
                case "Microsoft.Extensions.Logging.AzureAppServices.Internal.AzureAppServicesDiagnosticsLoggerProvider":
                    name = "AzureAppServices";
                    break;
                case "Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider":
                    name = "EventLog";
                    break;
                case "Microsoft.Extensions.Logging.TraceSource.TraceSourceLoggerProvider":
                    name = "TraceSource";
                    break;
                case "Microsoft.Extensions.Logging.EventSource.EventSourceLoggerProvider":
                    name = "EventSource";
                    break;
            }

            AddProvider(name, provider);
        }

        public void AddProvider(string providerName, ILoggerProvider provider)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            lock (_sync)
            {
                _providers = _providers.Concat(new[] { new KeyValuePair<ILoggerProvider, string>(provider, providerName) }).ToArray();
            }
        }

        public void AddFilter(string providerName, string categoryName, Func<LogLevel, bool> filter)
        {
            lock (_sync)
            {
                if (_categoryFilters.TryGetValue(categoryName, out var previousFilter))
                {
                    _categoryFilters[categoryName] = (currentProviderName, level) =>
                    {
                        if (previousFilter(currentProviderName, level))
                        {
                            if (string.Equals(providerName, currentProviderName))
                            {
                                return filter(level);
                            }

                            return true;
                        }

                        return false;
                    };
                }
                else
                {
                    _categoryFilters[categoryName] = (currentProviderName, level) =>
                    {
                        if (string.Equals(providerName, currentProviderName))
                        {
                            return filter(level);
                        }

                        return true;
                    };
                }
            }
        }

        public void AddFilter(string providerName, string categoryName, LogLevel minLevel)
        {
            lock (_sync)
            {
                if (_categoryFilters.TryGetValue(categoryName, out var previousFilter))
                {
                    _categoryFilters[categoryName] = (currentProviderName, level) =>
                    {
                        if (previousFilter(currentProviderName, level))
                        {
                            if (string.Equals(providerName, currentProviderName))
                            {
                                return level >= minLevel;
                            }

                            return true;
                        }

                        return false;
                    };
                }
                else
                {
                    _categoryFilters[categoryName] = (currentProviderName, level) =>
                    {
                        if (string.Equals(providerName, currentProviderName))
                        {
                            return level >= minLevel;
                        }

                        return true;
                    };
                }
            }
        }

        public void AddFilter(string providerName, Func<string, LogLevel, bool> filter)
        {
            lock (_sync)
            {
                if (_providerFilters.TryGetValue(providerName, out var value))
                {
                    _providerFilters[providerName] = (categoryName, level) =>
                    {
                        if (value(categoryName, level))
                        {
                            return filter(categoryName, level);
                        }

                        return false;
                    };
                }
                else
                {
                    _providerFilters[providerName] = (category, level) => filter(category, level);
                }
            }
        }

        public void AddFilter(string providerName, Func<LogLevel, bool> filter)
        {
            lock (_sync)
            {
                if (_categoryFilters.TryGetValue("Default", out var value))
                {
                    _categoryFilters["Default"] = (currentProviderName, level) =>
                    {
                        if (value(currentProviderName, level))
                        {
                            if (string.Equals(providerName, currentProviderName))
                            {
                                return filter(level);
                            }

                            return true;
                        }

                        return false;
                    };
                }
                else
                {
                    _categoryFilters["Default"] = (currentProviderName, level) =>
                    {
                        if (string.Equals(providerName, currentProviderName))
                        {
                            return filter(level);
                        }

                        return true;
                    };
                }
            }
        }

        public void AddFilter(Func<string, string, LogLevel, bool> filter)
        {
            lock (_sync)
            {
                var previousFilters = _genericFilters;
                _genericFilters = (providerName, category, level) =>
                {
                    if (previousFilters(providerName, category, level))
                    {
                        return filter(providerName, category, level);
                    }

                    return false;
                };
            }
        }

        public void AddFilter(IDictionary<string, LogLevel> filter)
        {
            lock (_sync)
            {
                foreach (var kvp in filter)
                {
                    if (_categoryFilters.TryGetValue(kvp.Key, out var currentFilter))
                    {
                        _categoryFilters[kvp.Key] = (providerName, level) =>
                        {
                            if (currentFilter(providerName, level))
                            {
                                return level >= kvp.Value;
                            }

                            return false;
                        };
                    }
                    else
                    {
                        _categoryFilters[kvp.Key] = (providerName, level) => level >= kvp.Value;
                    }
                }
            }
        }

        public void AddFilter(string providerName, IDictionary<string, LogLevel> filter)
        {
            lock (_sync)
            {
                foreach (var kvp in filter)
                {
                    if (_categoryFilters.TryGetValue(kvp.Key, out var currentFilter))
                    {
                        _categoryFilters[kvp.Key] = (currentProviderName, level) =>
                        {
                            if (currentFilter(currentProviderName, level))
                            {
                                if (string.Equals(providerName, currentProviderName))
                                {
                                    return level >= kvp.Value;
                                }

                                return true;
                            }

                            return false;
                        };
                    }
                    else
                    {
                        _categoryFilters[kvp.Key] = (currentProviderName, level) =>
                        {
                            if (string.Equals(providerName, currentProviderName))
                            {
                                return level >= kvp.Value;
                            }

                            return true;
                        };
                    }
                }
            }
        }

        public void AddFilter(Func<string, bool> providerNames, IDictionary<string, LogLevel> filter)
        {
            lock (_sync)
            {
                foreach (var kvp in filter)
                {
                    if (_categoryFilters.TryGetValue(kvp.Key, out var currentFilter))
                    {
                        _categoryFilters[kvp.Key] = (providerName, level) =>
                        {
                            if (providerNames(providerName))
                            {
                                if (currentFilter(providerName, level))
                                {
                                    return level >= kvp.Value;
                                }

                                return false;
                            }

                            return true;
                        };
                    }
                    else
                    {
                        _categoryFilters[kvp.Key] = (providerName, level) =>
                        {
                            if (providerNames(providerName))
                            {
                                return level >= kvp.Value;
                            }

                            return true;
                        };
                    }
                }
            }
        }

        // TODO: Figure out how to do this better, perhaps a new IConfigurableLogger interface?
        public IConfiguration Configuration => _configuration;

        internal KeyValuePair<ILoggerProvider, string>[] GetProviders()
        {
            return _providers;
        }

        internal bool IsEnabled(List<string> providerNames, string categoryName, LogLevel currentLevel)
        {
            if (_genericFilters != _trueFilter || _providerFilters.Count > 0)
            {
                foreach (var providerName in providerNames)
                {
                    if (string.IsNullOrEmpty(providerName))
                    {
                        continue;
                    }

                    if (_providerFilters.TryGetValue(providerName, out var filter))
                    {
                        if (!filter(categoryName, currentLevel))
                        {
                            return false;
                        }
                    }

                    if (_genericFilters != _trueFilter)
                    {
                        // filters from factory.AddFilter(Func<string, string, LogLevel, bool>)
                        if (!_genericFilters(providerName, categoryName, currentLevel))
                        {
                            return false;
                        }
                    }
                }
            }

            if (_configuration != null)
            {
                // need to loop over this separately because _filters can apply to multiple providerNames
                // but the configuration prefers early providerNames and will early out if a match is found
                foreach (var providerName in providerNames)
                {
                    // TODO: Caching?
                    var logLevelSection = _configuration.GetSection($"{providerName}:LogLevel");
                    if (logLevelSection != null)
                    {
                        foreach (var prefix in GetKeyPrefixes(categoryName))
                        {
                            if (TryGetSwitch(logLevelSection[prefix], out var configLevel))
                            {
                                return currentLevel >= configLevel;
                            }
                        }
                    }
                }
            }

            if (_defaultFilter == null)
            {
                return true;
            }

            // get a local reference to the filter so that if the config is reloaded then `_defaultFilter`
            // doesn't change while we are accessing it
            var localDefaultFilter = _defaultFilter;

            // No specific filter for this logger, check defaults
            foreach (var prefix in GetKeyPrefixes(categoryName))
            {
                if (localDefaultFilter.TryGetValue(prefix, out var defaultLevel))
                {
                    return currentLevel >= defaultLevel;
                }
            }

            return true;
        }

        private void OnConfigurationReload(object state)
        {
            _changeToken = _configuration.GetReloadToken();
            try
            {
                LoadDefaultConfigValues();
            }
            catch (Exception /*ex*/)
            {
                // TODO: Can we do anything?
                //Console.WriteLine($"Error while loading configuration changes.{Environment.NewLine}{ex}");
            }
            finally
            {
                // The token will change each time it reloads, so we need to register again.
                _changeToken.RegisterChangeCallback(OnConfigurationReload, null);
            }
        }

        private static bool TryGetSwitch(string value, out LogLevel level)
        {
            if (string.IsNullOrEmpty(value))
            {
                level = LogLevel.None;
                return false;
            }
            else if (Enum.TryParse(value, out level))
            {
                return true;
            }
            else
            {
                var message = $"Configuration value '{value}' is not supported.";
                throw new InvalidOperationException(message);
            }
        }

        private static IEnumerable<string> GetKeyPrefixes(string name)
        {
            while (!string.IsNullOrEmpty(name))
            {
                yield return name;
                var lastIndexOfDot = name.LastIndexOf('.');
                if (lastIndexOfDot == -1)
                {
                    yield return "Default";
                    break;
                }
                name = name.Substring(0, lastIndexOfDot);
            }
        }

        private void LoadDefaultConfigValues()
        {
            var replacementDefaultFilters = new Dictionary<string, LogLevel>();
            var logLevelSection = _configuration.GetSection("LogLevel");

            if (logLevelSection != null)
            {
                foreach (var section in logLevelSection.AsEnumerable(true))
                {
                    if (TryGetSwitch(section.Value, out var level))
                    {
                        replacementDefaultFilters[section.Key] = level;
                    }
                }
            }

            _defaultFilter = replacementDefaultFilters;
        }

        /// <summary>
        /// Check if the factory has been disposed.
        /// </summary>
        /// <returns>True when <see cref="Dispose"/> as been called</returns>
        protected virtual bool CheckDisposed() => _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                foreach (var provider in _providers)
                {
                    try
                    {
                        provider.Key.Dispose();
                    }
                    catch
                    {
                        // Swallow exceptions on dispose
                    }
                }
            }
        }
    }
}
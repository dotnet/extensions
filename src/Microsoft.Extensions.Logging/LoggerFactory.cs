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
        private IConfiguration _configuration;
        private IChangeToken _changeToken;
        private IDisposable _changeTokenRegistration;
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

            UseConfiguration(configuration);
        }

        /// <summary>
        /// Replaces the <see cref="IConfiguration"/> used for filtering.
        /// </summary>
        /// <param name="configuration">The new configuration to use.</param>
        /// <returns>The <see cref="LoggerFactory"/> so that additional calls can be chained.</returns>
        public LoggerFactory UseConfiguration(IConfiguration configuration)
        {
            if (configuration == _configuration)
            {
                return this;
            }

            // unregister the previous configuration callback if there was one
            _changeTokenRegistration?.Dispose();

            _configuration = configuration;

            if (configuration == null)
            {
                _changeToken = null;
                _changeTokenRegistration = null;
            }
            else
            {
                _changeToken = _configuration.GetReloadToken();
                _changeTokenRegistration = _changeToken?.RegisterChangeCallback(OnConfigurationReload, null);
            }

            LoadDefaultConfigValues();

            return this;
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

                foreach (var logger in _loggers)
                {
                    logger.Value.AddProvider(providerName, provider);
                }
            }
        }

        /// <summary>
        /// Adds a filter that applies to <paramref name="providerName"/> and <paramref name="categoryName"/> with the given
        /// <paramref name="filter"/>.
        /// </summary>
        /// <param name="providerName">The name of the provider.</param>
        /// <param name="categoryName">The name of the logger category.</param>
        /// <param name="filter">The filter that applies to logs for <paramref name="providerName"/> and <paramref name="categoryName"/>.
        /// Returning true means allow log through, false means reject log.</param>
        /// <returns>The <see cref="LoggerFactory"/> so that additional calls can be chained.</returns>
        public LoggerFactory AddFilter(string providerName, string categoryName, Func<LogLevel, bool> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

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

            return this;
        }

        /// <summary>
        /// Adds a filter that applies to <paramref name="providerName"/> with the given <paramref name="filter"/>.
        /// </summary>
        /// <param name="providerName">The name of the provider.</param>
        /// <param name="filter">The filter that applies to logs for <paramref name="providerName"/>.
        /// The string argument is the category being logged to.
        /// Returning true means allow log through, false means reject log.</param>
        /// <returns>The <see cref="LoggerFactory"/> so that additional calls can be chained.</returns>
        public LoggerFactory AddFilter(string providerName, Func<string, LogLevel, bool> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

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

            return this;
        }

        /// <summary>
        /// Adds a filter that applies to all logs.
        /// </summary>
        /// <param name="filter">The filter that applies to logs.
        /// The first string is the provider name and the second string is the category name being logged to.
        /// Returning true means allow log through, false means reject log.</param>
        /// <returns>The <see cref="LoggerFactory"/> so that additional calls can be chained.</returns>
        public LoggerFactory AddFilter(Func<string, string, LogLevel, bool> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

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

            return this;
        }

        /// <summary>
        /// Adds a filter to all logs.
        /// </summary>
        /// <param name="filter">The filter that applies to logs.
        /// The key is the category and the <see cref="LogLevel"/> is the minimum level allowed.</param>
        /// <returns>The <see cref="LoggerFactory"/> so that additional calls can be chained.</returns>
        public LoggerFactory AddFilter(IDictionary<string, LogLevel> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

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

            return this;
        }

        /// <summary>
        /// Adds a filter that applies to <paramref name="providerName"/> and <paramref name="categoryName"/>, allowing logs with the given
        /// minimum <see cref="LogLevel"/> or higher.
        /// </summary>
        /// <param name="providerName">The name of the provider.</param>
        /// <param name="categoryName">The name of the logger category.</param>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> that logs from
        /// <paramref name="providerName"/> and <paramref name="categoryName"/> are allowed.</param>
        public LoggerFactory AddFilter(string providerName, string categoryName, LogLevel minLevel)
        {
            return AddFilter(providerName, categoryName, level => level >= minLevel);
        }

        /// <summary>
        /// Adds a filter that applies to <paramref name="providerName"/> with the given
        /// <paramref name="filter"/>.
        /// </summary>
        /// <param name="providerName">The name of the provider.</param>
        /// <param name="filter">The filter that applies to logs for <paramref name="providerName"/>.
        /// Returning true means allow log through, false means reject log.</param>
        public LoggerFactory AddFilter(string providerName, Func<LogLevel, bool> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            // Using 'Default' for the category name means this filter will apply for all category names
            return AddFilter(providerName, "Default", filter);
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
                _changeTokenRegistration = _changeToken.RegisterChangeCallback(OnConfigurationReload, null);
            }
        }

        private static bool TryGetSwitch(string value, out LogLevel level)
        {
            if (string.IsNullOrEmpty(value))
            {
                level = LogLevel.None;
                return false;
            }
            else if (Enum.TryParse(value, true, out level))
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
            if (_configuration == null)
            {
                _defaultFilter = replacementDefaultFilters;
                return;
            }

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

                _changeTokenRegistration?.Dispose();

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
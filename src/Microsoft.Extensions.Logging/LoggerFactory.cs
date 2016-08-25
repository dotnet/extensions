// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Summary description for LoggerFactory
    /// </summary>
    public class LoggerFactory : ILoggerFactory
    {
        private readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>(StringComparer.Ordinal);
        private ILoggerProvider[] _providers = new ILoggerProvider[0];
        private readonly object _sync = new object();
        private volatile bool _disposed;

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
                    logger = new Logger(this, categoryName);
                    _loggers[categoryName] = logger;
                }
            }
            return logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            lock (_sync)
            {
                _providers = _providers.Concat(new[] { provider }).ToArray();
                foreach (var logger in _loggers)
                {
                    logger.Value.AddProvider(provider);
                }
            }
        }

        internal ILoggerProvider[] GetProviders()
        {
            return _providers;
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
                        provider.Dispose();
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
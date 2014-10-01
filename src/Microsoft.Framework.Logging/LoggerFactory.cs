// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// Summary description for LoggerFactory
    /// </summary>
    public class LoggerFactory : ILoggerFactory
    {
        private readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>(StringComparer.Ordinal);
        private ILoggerProvider[] _providers = new ILoggerProvider[0];
        private readonly object _sync = new object();

        public ILogger Create(string name)
        {
            Logger logger;
            lock (_sync)
            {
                if (!_loggers.TryGetValue(name, out logger))
                {
                    logger = new Logger(this, name);
                    _loggers[name] = logger;
                }
            }
            return logger;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            lock (_sync)
            {
                _providers = _providers.Concat(new[] { provider }).ToArray();
                foreach(var logger in _loggers)
                {
                    logger.Value.AddProvider(provider);
                }
            }
        }

        internal ILoggerProvider[] GetProviders()
        {
            return _providers;
        }
    }
}
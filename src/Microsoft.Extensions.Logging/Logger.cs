// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions.Internal;

namespace Microsoft.Extensions.Logging
{
    internal class Logger : ILogger
    {
        private readonly LoggerFactory _loggerFactory;
        private readonly string _categoryName;
        private LoggerInformation[] _loggers;
        private readonly Func<string, LogLevel, bool> _categoryFilter;

        public Logger(LoggerFactory loggerFactory, string categoryName, Func<string, LogLevel, bool> categoryFilter)
        {
            _loggerFactory = loggerFactory;
            _categoryName = categoryName;

            var providers = loggerFactory.GetProviders();
            if (providers.Length > 0)
            {
                _loggers = new LoggerInformation[providers.Length];
                for (var index = 0; index < providers.Length; index++)
                {
                    _loggers[index] = new LoggerInformation
                    {
                        Logger = providers[index].Key.CreateLogger(categoryName),
                        // Order of preference
                        // 1. Custom Name
                        // 2. Provider FullName
                        ProviderNames = new List<string>
                        {
                            providers[index].Value,
                            providers[index].Key.GetType().FullName
                        }
                    };
                }
            }

            _categoryFilter = categoryFilter;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (_loggers == null)
            {
                return;
            }

            List<Exception> exceptions = null;
            foreach (var loggerInfo in _loggers)
            {
                // TODO: Try to noop if no filters set
                if (!_categoryFilter(loggerInfo.ProviderNames[0], logLevel) ||
                    !_categoryFilter(loggerInfo.ProviderNames[1], logLevel))
                {
                    continue;
                }

                // checks config and filters set on the LoggerFactory
                if (!_loggerFactory.IsEnabled(loggerInfo.ProviderNames, _categoryName, logLevel))
                {
                    continue;
                }

                try
                {
                    loggerInfo.Logger.Log(logLevel, eventId, state, exception, formatter);
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (_loggers == null)
            {
                return false;
            }

            List<Exception> exceptions = null;
            foreach (var loggerInfo in _loggers)
            {
                if (!_categoryFilter(loggerInfo.ProviderNames[0], logLevel) ||
                    !_categoryFilter(loggerInfo.ProviderNames[1], logLevel))
                {
                    continue;
                }

                // checks config and filters set on the LoggerFactory
                if (!_loggerFactory.IsEnabled(loggerInfo.ProviderNames, _categoryName, logLevel))
                {
                    continue;
                }

                try
                {
                    if (loggerInfo.Logger.IsEnabled(logLevel))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).",
                    innerExceptions: exceptions);
            }

            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (_loggers == null)
            {
                return NullScope.Instance;
            }

            if (_loggers.Length == 1)
            {
                return _loggers[0].Logger.BeginScope(state);
            }

            var loggers = _loggers;

            var scope = new Scope(loggers.Length);
            List<Exception> exceptions = null;
            for (var index = 0; index < loggers.Length; index++)
            {
                try
                {
                    var disposable = loggers[index].Logger.BeginScope(state);
                    scope.SetDisposable(index, disposable);
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
            }

            return scope;
        }

        private class Scope : IDisposable
        {
            private bool _isDisposed;

            private IDisposable _disposable0;
            private IDisposable _disposable1;
            private readonly IDisposable[] _disposable;

            public Scope(int count)
            {
                if (count > 2)
                {
                    _disposable = new IDisposable[count - 2];
                }
            }

            public void SetDisposable(int index, IDisposable disposable)
            {
                if (index == 0)
                {
                    _disposable0 = disposable;
                }
                else if (index == 1)
                {
                    _disposable1 = disposable;
                }
                else
                {
                    _disposable[index - 2] = disposable;
                }
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    if (_disposable0 != null)
                    {
                        _disposable0.Dispose();
                    }
                    if (_disposable1 != null)
                    {
                        _disposable1.Dispose();
                    }
                    if (_disposable != null)
                    {
                        var count = _disposable.Length;
                        for (var index = 0; index != count; ++index)
                        {
                            if (_disposable[index] != null)
                            {
                                _disposable[index].Dispose();
                            }
                        }
                    }

                    _isDisposed = true;
                }
            }

            internal void Add(IDisposable disposable)
            {
                throw new NotImplementedException();
            }
        }

        private struct LoggerInformation
        {
            public ILogger Logger { get; set; }
            public List<string> ProviderNames { get; set; }
        }
    }
}
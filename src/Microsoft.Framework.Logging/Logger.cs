// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.Framework.Logging
{
    internal class Logger : ILogger
    {
        private readonly LoggerFactory _loggerFactory;
        private readonly string _name;
        private ILogger[] _loggers = new ILogger[0];

        public Logger(LoggerFactory loggerFactory, string name)
        {
            _loggerFactory = loggerFactory;
            _name = name;

            var providers = loggerFactory.GetProviders();
            _loggers = new ILogger[providers.Length];
            for (var index = 0; index != providers.Length; index++)
            {
                _loggers[index] = providers[index].CreateLogger(name);
            }
        }

        public void Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            if (logLevel >= _loggerFactory.MinimumLevel)
            {
                foreach (var logger in _loggers)
                {
                    logger.Log(logLevel, eventId, state, exception, formatter);
                }
            }
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel < _loggerFactory.MinimumLevel)
            {
                return false;
            }
            foreach (var logger in _loggers)
            {
                if (logger.IsEnabled(logLevel))
                {
                    return true;
                }
            }
            return false;
        }

        public IDisposable BeginScope(object state)
        {
            var loggers = _loggers;
            var scope = new Scope(loggers.Length);
            for (var index = 0; index != loggers.Length; index++)
            {
                scope.SetDisposable(index, loggers[index].BeginScope(state));
            }
            return scope;
        }

        internal void AddProvider(ILoggerProvider provider)
        {
            var logger = provider.CreateLogger(_name);
            _loggers = _loggers.Concat(new[] { logger }).ToArray();
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

            protected virtual void Dispose(bool disposing)
            {
                if (!_isDisposed)
                {
                    if (disposing)
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
                    }

                    _isDisposed = true;
                }
            }

            // This code added to correctly implement the disposable pattern.
            public void Dispose()
            {
                // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
                Dispose(true);
                // TODO: tell GC not to call its finalizer when the above finalizer is overridden.
                // GC.SuppressFinalize(this);
            }

            internal void Add(IDisposable disposable)
            {
                throw new NotImplementedException();
            }
        }
    }
}
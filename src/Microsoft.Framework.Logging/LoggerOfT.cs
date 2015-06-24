// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Framework.Logging
{
    /// <summary>
    /// Delegates to a new <see cref="ILogger"/> instance using the full name of the given type, created by the
    /// provided <see cref="ILoggerFactory"/>.
    /// </summary>
    /// <typeparam name="T">The type.</typeparam>
    public class Logger<T> : ILogger<T>
    {
        private readonly ILogger _logger;
        
        /// <summary>
        /// Creates a new <see cref="Logger{T}"/>.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public Logger(ILoggerFactory factory)
        {
            _logger = factory.CreateLogger<T>();
        }

        IDisposable ILogger.BeginScopeImpl(object state)
        {
            return _logger.BeginScopeImpl(state);
        }

        bool ILogger.IsEnabled(LogLevel logLevel)
        {
            return _logger.IsEnabled(logLevel);
        }

        void ILogger.Log(LogLevel logLevel, int eventId, object state, Exception exception, Func<object, Exception, string> formatter)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
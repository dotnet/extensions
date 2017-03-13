// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging.EventSource;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for the <see cref="ILoggerFactory"/> class.
    /// </summary>
    public static class EventSourceLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds an event logger.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        public static LoggerFactory AddEventSourceLogger(this LoggerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var loggerProvider = LoggingEventSource.Instance.CreateLoggerProvider();
            factory.AddProvider("EventSource", loggerProvider);

            return factory;
        }

        /// <summary>
        /// Adds an event logger that is enabled for <see cref="LogLevel"/>.Information or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        public static ILoggerFactory AddEventSourceLogger(this ILoggerFactory factory)
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var loggerProvider = LoggingEventSource.Instance.CreateLoggerProvider();
            factory.AddProvider(loggerProvider);

            return factory;
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.EventSource;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for the <see cref="ILoggerFactory"/> class.
    /// </summary>
    public static class EventSourceLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds an event logger named 'EventSource' to the factory.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        public static ILoggingBuilder AddEventSourceLogger(this ILoggingBuilder builder)
        {
            return AddEventSourceLogger(builder, null);
        }

        /// <summary>
        /// Adds an event logger named 'EventSource' to the factory.
        /// </summary>
        /// <param name="builder">The extension method argument.</param>
        /// <param name="providerName">Events will be logged using the specified provider name</param>
        public static ILoggingBuilder AddEventSourceLogger(this ILoggingBuilder builder, string providerName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Services.TryAddSingleton((_) => new LoggingEventSource(providerName));
            builder.Services.TryAddSingleton(ServiceDescriptor.Singleton<EventSourceLogger, EventSourceLogger>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, EventSourceLoggerProvider>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>, EventLogFiltersConfigureOptions>());
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IOptionsChangeTokenSource<LoggerFilterOptions>, EventLogFiltersConfigureOptionsChangeSource>());
            return builder;
        }

    }
}

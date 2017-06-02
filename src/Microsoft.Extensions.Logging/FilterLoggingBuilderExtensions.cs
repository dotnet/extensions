// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for setting up logging services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class FilterLoggingBuilderExtensions
    {
        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider
        {
            return AddRule(builder, type: typeof(T).FullName, filter: (type, name, level) => levelFilter(level));
        }

        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, string category, LogLevel level)
        {
            return AddRule(builder, category: category, level: level);
        }

        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, string category, LogLevel level) where T: ILoggerProvider
        {
            return AddRule(builder, type: typeof(T).FullName, category: category, level: level);
        }

        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, Func<string, string, LogLevel, bool> filter)
        {
            return AddRule(builder, filter: filter);
        }

        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, Func<string, string, LogLevel, bool> filter) where T : ILoggerProvider
        {
            return AddRule(builder, type: typeof(T).FullName, filter: filter);
        }

        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, Func<string, LogLevel, bool> categoryLevelFilter)
        {
            return AddRule(builder, filter: (type, name, level) => categoryLevelFilter(name, level));
        }

        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, Func<string, LogLevel, bool> categoryLevelFilter) where T : ILoggerProvider
        {
            return AddRule(builder, type: typeof(T).FullName, filter: (type, name, level) => categoryLevelFilter(name, level));
        }

        public static ILoggingBuilder AddFilter(this ILoggingBuilder builder, string category, Func<LogLevel, bool> levelFilter)
        {
            return AddRule(builder, category: category, filter: (type, name, level) => levelFilter(level));
        }

        public static ILoggingBuilder AddFilter<T>(this ILoggingBuilder builder, string category, Func<LogLevel, bool> levelFilter) where T : ILoggerProvider
        {
            return AddRule(builder, type: typeof(T).FullName, category: category, filter: (type, name, level) => levelFilter(level));
        }

        private static ILoggingBuilder AddRule(ILoggingBuilder builder,
            string type = null,
            string category = null,
            LogLevel? level = null,
            Func<string, string, LogLevel, bool> filter = null)
        {
            builder.Services.Configure<LoggerFilterOptions>(options => options.Rules.Add(new LoggerFilterRule(type, category, level, filter)));
            return builder;
        }
    }
}
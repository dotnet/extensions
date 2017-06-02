// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Logging
{
    /// <summary>
    /// Extension methods for setting up logging services in an <see cref="ILoggingBuilder" />.
    /// </summary>
    public static class LoggingBuilderExtensions
    {
        public static ILoggingBuilder AddConfiguration(this ILoggingBuilder builder, IConfiguration configuration)
        {
            builder.Services.AddSingleton<IConfigureOptions<LoggerFilterOptions>>(new LoggerFilterConfigureOptions(configuration));
            builder.Services.AddSingleton<IOptionsChangeTokenSource<LoggerFilterOptions>>(new ConfigurationChangeTokenSource<LoggerFilterOptions>(configuration));

            return builder;
        }

        public static ILoggingBuilder SetMinimumLevel(this ILoggingBuilder builder, LogLevel level)
        {
            builder.Services.Add(ServiceDescriptor.Singleton<IConfigureOptions<LoggerFilterOptions>>(
                new DefaultLoggerLevelConfigureOptions(level)));
            return builder;
        }

        public static ILoggingBuilder AddProvider(this ILoggingBuilder builder, ILoggerProvider provider)
        {
            builder.Services.AddSingleton(provider);
            return builder;
        }
    }
}
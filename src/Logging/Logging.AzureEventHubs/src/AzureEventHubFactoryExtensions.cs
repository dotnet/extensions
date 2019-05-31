// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.AzureEventHubs;
using Microsoft.Extensions.Logging.Configuration;

namespace Microsoft.Extensions.Logging
{
    public static class AzureEventHubFactoryExtensions
    {
        /// <summary>
        /// Adds a AzureEventHubs logger named 'AzureEventHubs' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        public static ILoggingBuilder AddAzureEventHubs(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, AzureEventHubsLoggerProvider>());
            builder.Services.TryAddSingleton(ServiceDescriptor.Singleton<IAzureEventHubsLoggerFormatter, DefaultAzureEventHubsLoggerFormatter>());
            builder.Services.TryAddSingleton(ServiceDescriptor.Singleton<IAzureEventHubsLoggerProcessor, DefaultAzureEventHubsLoggerProcessor>());
            LoggerProviderOptions.RegisterProviderOptions<AzureEventHubsLoggerOptions, AzureEventHubsLoggerProvider>(builder.Services);
            return builder;
        }

        /// <summary>
        /// Adds a AzureEventHubs logger named 'AzureEventHubs' to the factory.
        /// </summary>
        /// <param name="builder">The <see cref="ILoggingBuilder"/> to use.</param>
        /// <param name="configure"></param>
        public static ILoggingBuilder AddAzureEventHubs(this ILoggingBuilder builder, Action<AzureEventHubsLoggerOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            builder.AddAzureEventHubs();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}

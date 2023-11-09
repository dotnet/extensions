// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Logging.Test;

internal static class Utils
{
    public static ILoggerFactory CreateLoggerFactory(Action<ILoggingBuilder>? configure = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.EnableEnrichment();
            builder.EnableRedaction();

            configure?.Invoke(builder);
        });
        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
        ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        return new DisposingLoggerFactory(loggerFactory, serviceProvider);
    }

    private sealed class DisposingLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        private readonly ServiceProvider _serviceProvider;

        public DisposingLoggerFactory(ILoggerFactory loggerFactory, ServiceProvider serviceProvider)
        {
            _loggerFactory = loggerFactory;
            _serviceProvider = serviceProvider;
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggerFactory.CreateLogger(categoryName);
        }

        public void AddProvider(ILoggerProvider provider)
        {
            _loggerFactory.AddProvider(provider);
        }
    }
}

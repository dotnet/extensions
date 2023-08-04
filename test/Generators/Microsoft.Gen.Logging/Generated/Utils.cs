// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using TestClasses;

namespace Microsoft.Gen.Logging.Test;

internal static class Utils
{
    private class Provider : ILoggerProvider
    {
        private readonly ILogger _logger;

        public Provider(ILogger logger)
        {
            _logger = logger;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _logger;
        }

        public void Dispose()
        {
            // nothing
        }
    }

    public class TestLogger : ILogger, IDisposable
    {
        private readonly ILogger _logger;
        private readonly ServiceProvider _serviceProvider;

        public TestLogger(ILogger logger, ServiceProvider serviceProvider, FakeLogger fakeLogger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            FakeLogger = fakeLogger;
            FakeLogCollector = fakeLogger.Collector;
        }

        public FakeLogCollector FakeLogCollector { get; }
        public FakeLogger FakeLogger { get; }

        public void Dispose()
        {
            _serviceProvider.Dispose();
            GC.SuppressFinalize(this);
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => _logger.BeginScope(state);
        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);
    }

    public static TestLogger GetLogger()
    {
        var fakeLogger = new FakeLogger();

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddProvider(new Provider(fakeLogger));
            builder.EnableRedaction();
        });

        serviceCollection.AddRedaction(builder =>
        {
            builder.SetFallbackRedactor<StarRedactor>();
        });

        var serviceProvider = serviceCollection.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(Utils));
        return new TestLogger(logger, serviceProvider, fakeLogger);
    }

    public static string? GetValue(this IReadOnlyList<KeyValuePair<string, string>> state, string key)
    {
        foreach (var kvp in state)
        {
            if (kvp.Key == key)
            {
                return kvp.Value;
            }
        }

        return null;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

// dummy implementation for collecting test output
internal class LogCollector : ILoggerProvider
{
    public (string categoryName, LogLevel logLevel, EventId eventId, Exception? exception, string message)[] ToArray()
    {
        lock (_items)
        {
            return _items.ToArray();
        }
    }

    public void WriteTo(ITestOutputHelper log)
    {
        lock (_items)
        {
            foreach (var logItem in _items)
            {
                log.WriteLine($"{logItem.categoryName} {logItem.eventId}: {logItem.message}");
            }
        }
    }

    public void AssertErrors(int[] errorIds)
    {
        lock (_items)
        {
            bool same;
            if (errorIds.Length == _items.Count)
            {
                int index = 0;
                same = true;
                foreach (var item in _items)
                {
                    if (item.eventId.Id != errorIds[index++])
                    {
                        same = false;
                        break;
                    }
                }
            }
            else
            {
                same = false;
            }
            if (!same)
            {
                // we expect this to fail, then
                Assert.Equal(string.Join(",", errorIds), string.Join(",", _items.Select(static x => x.eventId.Id)));
            }
        }
    }

    private readonly List<(string categoryName, LogLevel logLevel, EventId eventId, Exception? exception, string message)> _items = [];

    ILogger ILoggerProvider.CreateLogger(string categoryName) => new TypedLogCollector(this, categoryName);

    void IDisposable.Dispose()
    {
        // nothing to do
    }

    private sealed class TypedLogCollector(LogCollector parent, string categoryName) : ILogger
    {
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        bool ILogger.IsEnabled(LogLevel logLevel) => true;
        void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (parent._items)
            {
                parent._items.Add((categoryName, logLevel, eventId, exception, formatter(state, exception)));
            }
        }
    }
}

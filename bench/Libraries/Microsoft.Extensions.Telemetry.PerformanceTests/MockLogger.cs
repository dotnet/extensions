// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Bench;

/// <summary>
/// A logger which captures the last log state logged to it.
/// </summary>
internal sealed class MockLogger : ILogger
{
    private sealed class Disposable : IDisposable
    {
        public void Dispose()
        {
            // nothing to do
        }
    }

#pragma warning disable CS8633
#pragma warning disable CS8766
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
#pragma warning restore CS8633
#pragma warning restore CS8766
    {
        return new Disposable();
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        switch (state)
        {
            case IReadOnlyList<KeyValuePair<string, object?>> l:
            {
                for (int i = 0; i < l.Count; i++)
                {
                    _ = ProcessItem(l[i]);
                }

                break;
            }

            case IEnumerable<KeyValuePair<string, object?>> enumerable:
            {
                foreach (var e in enumerable)
                {
                    _ = ProcessItem(e);
                }

                break;
            }
        }
    }

    private static object? ProcessItem(KeyValuePair<string, object?> item)
    {
        var o = item.Value;

        if (o?.GetType() == typeof(Guid))
        {
            // simulate what a real exporter like OTel would do.
            _ = o.ToString();
        }

        return o;
    }
}

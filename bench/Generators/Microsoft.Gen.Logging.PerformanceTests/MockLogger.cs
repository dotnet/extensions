// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Telemetry.Logging;
using Microsoft.Shared.Pools;

namespace Microsoft.Gen.Logging.Bench;

/// <summary>
/// A logger which captures the last log state logged to it.
/// </summary>
internal sealed class MockLogger : ILogger
{
    private readonly ObjectPool<List<KeyValuePair<string, object?>>> _listPool = PoolFactory.CreateListPool<KeyValuePair<string, object?>>();

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

    public bool IsEnabled(LogLevel logLevel)
    {
        return Enabled;
    }

    public bool Enabled { get; set; }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        switch (state)
        {
            case LogMethodHelper helper:
            {
                // this path is optimized in the real Logger implementation, so it is here too...
                break;
            }

            case IEnumerable<KeyValuePair<string, object?>> enumerable:
            {
                var l = _listPool.Get();

                foreach (var e in enumerable)
                {
                    // Any non-primitive value type will be turned into a string on this path.
                    // But when using the generated code, this conversion to string happens in the
                    // generated code, which eliminates the overhead of boxing the value type.
                    if (e.Value is Guid)
                    {
                        _ = e.Value.ToString();
                    }

                    l.Add(e);
                }

                _listPool.Return(l);
                break;
            }
        }
    }
}

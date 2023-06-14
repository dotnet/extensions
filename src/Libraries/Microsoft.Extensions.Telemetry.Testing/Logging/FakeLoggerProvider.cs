// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Testing.Logging;

// bug in .NET 7 preview analyzer
#pragma warning disable CA1063

/// <summary>
/// A provider of fake loggers.
/// </summary>
[ProviderAlias("Fake")]
public class FakeLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentDictionary<string, FakeLogger> _loggers = new();
    private IExternalScopeProvider _scopeProvider = new LoggerExternalScopeProvider();

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeLoggerProvider"/> class.
    /// </summary>
    /// <param name="collector">The collector that will receive all log records emitted to fake loggers.</param>
    public FakeLoggerProvider(FakeLogCollector? collector = null)
    {
        Collector = collector ?? new FakeLogCollector();
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="FakeLoggerProvider"/> class.
    /// </summary>
    [ExcludeFromCodeCoverage]
    ~FakeLoggerProvider()
    {
        Dispose(false);
    }

    /// <summary>
    /// Sets external scope information source for logger provider.
    /// </summary>
    /// <param name="scopeProvider">The provider of scope data.</param>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = Throw.IfNull(scopeProvider);

        foreach (var entry in _loggers)
        {
            entry.Value.ScopeProvider = _scopeProvider;
        }
    }

    /// <summary>
    /// Creates a new <see cref="ILogger"/> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="ILogger"/> that was created.</returns>
    ILogger ILoggerProvider.CreateLogger(string categoryName) => CreateLogger(categoryName);

    /// <summary>
    /// Creates a new <see cref="FakeLogger"/> instance.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The instance of <see cref="ILogger"/> that was created.</returns>
    public FakeLogger CreateLogger(string? categoryName)
    {
        return _loggers.GetOrAdd(categoryName ?? string.Empty, (name) => new(Collector, name)
        {
            ScopeProvider = _scopeProvider,
        });
    }

    /// <summary>
    /// Clean up resources held by this object.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the log record collector for all loggers created by this provider.
    /// </summary>
    public FakeLogCollector Collector { get; }

    /// <summary>
    /// Clean up resources held by this object.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> when called from the <see cref="Dispose()"/> method, <see langword="false"/> when called from a finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    internal string FirstLoggerName => _loggers.First().Key;
}

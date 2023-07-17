// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Internal;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Microsoft.Extensions.Telemetry.Logging;

/// <summary>
/// OpenTelemetry Logger provider class.
/// </summary>
[ProviderAlias("R9")]
[Experimental(diagnosticId: Experiments.Telemetry, UrlFormat = Experiments.UrlFormat)]
public sealed class LoggerProvider : BaseProvider, ILoggerProvider, ISupportExternalScope
{
    private const int ProcessorShutdownGracePeriodInMs = 5000;
    private readonly ConcurrentDictionary<string, Logger> _loggers = new();
    private bool _disposed;
    private IExternalScopeProvider? _scopeProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerProvider" /> class.
    /// </summary>
    /// <param name="loggingOptions">Logger options.</param>
    /// <param name="enrichers">Collection of enrichers.</param>
    /// <param name="processors">Collection of processors.</param>
    internal LoggerProvider(
        IOptions<LoggingOptions> loggingOptions,
        IEnumerable<ILogEnricher> enrichers,
        IEnumerable<BaseProcessor<LogRecord>> processors)
    {
        var options = Throw.IfMemberNull(loggingOptions, loggingOptions.Value);

        // Accessing Sdk class https://github.com/open-telemetry/opentelemetry-dotnet/blob/7fd37833711e27a02e169de09f3816d1d9557be4/src/OpenTelemetry/Sdk.cs
        // is just to activate OpenTelemetry .NET SDK defaults along with its Self-Diagnostics.
        _ = Sdk.SuppressInstrumentation;

        SelfDiagnostics.EnsureInitialized();

        var allProcessors = processors.ToList();

        Processor = allProcessors.Count switch
        {
            0 => null,
            1 => allProcessors[0],
            _ => new CompositeProcessor<LogRecord>(allProcessors)
        };

        Enrichers = enrichers.ToArray();
        UseFormattedMessage = options.UseFormattedMessage;
        IncludeScopes = options.IncludeScopes;
        IncludeStackTrace = options.IncludeStackTrace;
        MaxStackTraceLength = options.MaxStackTraceLength;

        if (!allProcessors.Exists(p => p is BatchExportProcessor<LogRecord>))
        {
            CanUsePropertyBagPool = true;
        }
    }

    internal bool CanUsePropertyBagPool { get; }
    internal bool UseFormattedMessage { get; }
    internal bool IncludeScopes { get; }
    internal BaseProcessor<LogRecord>? Processor { get; }
    internal ILogEnricher[] Enrichers { get; }
    internal bool IncludeStackTrace { get; }
    internal int MaxStackTraceLength { get; }

    /// <summary>
    /// Sets external scope information source for logger provider.
    /// </summary>
    /// <param name="scopeProvider">scope provider object.</param>
    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;

        foreach (KeyValuePair<string, Logger> entry in _loggers)
        {
            if (entry.Value is Logger logger)
            {
                logger.ScopeProvider = _scopeProvider;
            }
        }
    }

    /// <summary>
    /// Creates a new Microsoft.Extensions.Logging.ILogger instance.
    /// </summary>
    /// <param name="categoryName">The category name for message produced by the logger.</param>
    /// <returns>ILogger object.</returns>
    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, static (name, t) => new Logger(name, t)
        {
            ScopeProvider = t._scopeProvider,
        }, this);
    }

    /// <summary>
    /// Performs tasks related to freeing up resources.
    /// </summary>
    /// <param name="disposing">Parameter indicating whether resources need disposing.</param>
    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _ = Processor?.Shutdown(ProcessorShutdownGracePeriodInMs);
            Processor?.Dispose();
        }

        _disposed = true;

        base.Dispose(disposing);
    }
}

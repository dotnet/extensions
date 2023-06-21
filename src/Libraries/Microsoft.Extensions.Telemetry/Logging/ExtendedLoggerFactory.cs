// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;

namespace Microsoft.Extensions.Telemetry.Logging;

internal sealed class ExtendedLoggerFactory : ILoggerFactory
{
    private readonly LoggerFactory _loggerFactory;
    private readonly ConcurrentDictionary<string, ILogger> _cache = new();
    private readonly IDisposable? _changeTokenRegistration;

    private readonly Action<IEnrichmentPropertyBag>[] _enrichers;
    private readonly KeyValuePair<string, object?>[] _staticProperties;
    private readonly Func<DataClassification, Redactor> _redactorProvider;

#pragma warning disable S107 // Methods should not have too many parameters
    public ExtendedLoggerFactory(
        IEnumerable<ILoggerProvider> providers,
        IOptionsMonitor<LoggerFilterOptions> filterOptions,
        IOptionsMonitor<LoggerEnrichmentOptions>? enrichmentOptions,
        IOptionsMonitor<LoggerRedactionOptions>? redactionOptions,
        IOptions<LoggerFactoryOptions>? factoryOptions,
        IExternalScopeProvider? scopeProvider,
        IEnumerable<ILogEnricher> enrichers,
        IEnumerable<IStaticLogEnricher> staticEnrichers,
        IRedactorProvider redactorProvider)
#pragma warning restore S107 // Methods should not have too many parameters
    {
        _loggerFactory = new LoggerFactory(providers, filterOptions, factoryOptions, scopeProvider);
        _enrichers = enrichers.Select<ILogEnricher, Action<IEnrichmentPropertyBag>>(e => e.Enrich).ToArray();
        _changeTokenRegistration = enrichmentOptions?.OnChange(UpdateStackTraceOptions);

        var provider = redactionOptions != null
            ? redactorProvider
            : NullRedactorProvider.Instance;
        _redactorProvider = provider.GetRedactor;

        var bag = new ExtendedLogger.PropertyBag();
        foreach (var enricher in staticEnrichers)
        {
            enricher.Enrich(bag);
        }

        _staticProperties = bag.ToArray();
        Config = ComputeConfig(enrichmentOptions?.CurrentValue);
    }

    /// <summary>
    /// Creates an <see cref="ILogger"/> with the given <paramref name="categoryName"/>.
    /// </summary>
    /// <param name="categoryName">The category name for messages produced by the logger.</param>
    /// <returns>The <see cref="ILogger"/> that was created.</returns>
    public ILogger CreateLogger(string categoryName)
        => _cache.GetOrAdd(categoryName, (name, lfp) => new ExtendedLogger(lfp._loggerFactory.CreateLogger(name), this), this);

    /// <summary>
    /// Adds the given provider to those used in creating <see cref="ILogger"/> instances.
    /// </summary>
    /// <param name="provider">The <see cref="ILoggerProvider"/> to add.</param>
    public void AddProvider(ILoggerProvider provider) => _loggerFactory.AddProvider(provider);

    /// <inheritdoc/>
    public void Dispose()
    {
        _loggerFactory.Dispose();
        _changeTokenRegistration?.Dispose();
    }

    /// <summary>
    /// Gets the current config state that loggers should use.
    /// </summary>
    /// <remarks>
    /// This gets replaced whenever option monitors trigger. The loggers should sample this value
    /// and use it for an entire call to ILogger.Log so as to get a consistent view of config for the
    /// execution span of the function.
    /// </remarks>
    internal LoggerConfig Config { get; private set; }

    private LoggerConfig ComputeConfig(LoggerEnrichmentOptions? enrichmentOptions)
    {
        return enrichmentOptions == null
            ? new(Array.Empty<KeyValuePair<string, object?>>(), Array.Empty<Action<IEnrichmentPropertyBag>>(), false, false, 0, _redactorProvider)
            : new(_staticProperties, _enrichers, enrichmentOptions.CaptureStackTraces, enrichmentOptions.UseFileInfoForStackTraces, enrichmentOptions.MaxStackTraceLength, _redactorProvider);
    }

    private void UpdateStackTraceOptions(LoggerEnrichmentOptions enrichmentOptions) => Config = ComputeConfig(enrichmentOptions);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Logging;

internal sealed class ExtendedLoggerFactory : ILoggerFactory
{
    private readonly Dictionary<string, ExtendedLogger> _loggers = new(StringComparer.Ordinal);
    private readonly List<ProviderRegistration> _providerRegistrations = [];
    private readonly object _sync = new();
    private readonly IDisposable? _filterOptionsChangeTokenRegistration;
    private readonly LoggerFactoryOptions _factoryOptions;
    private readonly IDisposable? _enrichmentOptionsChangeTokenRegistration;
    private readonly IDisposable? _redactionOptionsChangeTokenRegistration;
    private readonly Action<IEnrichmentTagCollector>[] _enrichers;
    private readonly KeyValuePair<string, object?>[] _staticTags;
    private readonly Func<DataClassification, Redactor> _redactorProvider;
    private volatile bool _disposed;
    private LoggerFilterOptions _filterOptions;
    private IExternalScopeProvider? _scopeProvider;

#pragma warning disable S107 // Methods should not have too many parameters
    public ExtendedLoggerFactory(
        IEnumerable<ILoggerProvider> providers,
        IEnumerable<ILogEnricher> enrichers,
        IEnumerable<IStaticLogEnricher> staticEnrichers,
        IOptionsMonitor<LoggerFilterOptions> filterOptions,
        IOptions<LoggerFactoryOptions>? factoryOptions = null,
        IExternalScopeProvider? scopeProvider = null,
        IOptionsMonitor<LoggerEnrichmentOptions>? enrichmentOptions = null,
        IOptionsMonitor<LoggerRedactionOptions>? redactionOptions = null,
        IRedactorProvider? redactorProvider = null)
#pragma warning restore S107 // Methods should not have too many parameters
    {
        _scopeProvider = scopeProvider;

        _factoryOptions = factoryOptions == null || factoryOptions.Value == null ? new LoggerFactoryOptions() : factoryOptions.Value;

        const ActivityTrackingOptions ActivityTrackingOptionsMask = ~(ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId |
                                                                      ActivityTrackingOptions.TraceFlags | ActivityTrackingOptions.TraceState | ActivityTrackingOptions.Tags
                                                                      | ActivityTrackingOptions.Baggage);

        if ((_factoryOptions.ActivityTrackingOptions & ActivityTrackingOptionsMask) != 0)
        {
            Throw.ArgumentException($"{_factoryOptions.ActivityTrackingOptions} is invalid ActivityTrackingOptions value.", nameof(factoryOptions));
        }

        foreach (ILoggerProvider p in providers)
        {
            AddProviderRegistration(p, dispose: false);
        }

        _filterOptionsChangeTokenRegistration = filterOptions.OnChange(RefreshFilters);
        RefreshFilters(filterOptions.CurrentValue);

        _enrichers = enrichers.Select<ILogEnricher, Action<IEnrichmentTagCollector>>(e => e.Enrich).ToArray();
        _enrichmentOptionsChangeTokenRegistration = enrichmentOptions?.OnChange(UpdateEnrichmentOptions);
        _redactionOptionsChangeTokenRegistration = redactionOptions?.OnChange(UpdateRedactionOptions);

        var provider = redactionOptions != null && redactorProvider != null
            ? redactorProvider
            : NullRedactorProvider.Instance;
        _redactorProvider = provider.GetRedactor;

        var tags = new List<KeyValuePair<string, object?>>();
        var collector = new ExtendedLogger.EnrichmentTagCollector(tags);
        foreach (var enricher in staticEnrichers)
        {
            enricher.Enrich(collector);
        }

        _staticTags = [.. tags];
        Config = ComputeConfig(enrichmentOptions?.CurrentValue, redactionOptions?.CurrentValue);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            _filterOptionsChangeTokenRegistration?.Dispose();
            _enrichmentOptionsChangeTokenRegistration?.Dispose();
            _redactionOptionsChangeTokenRegistration?.Dispose();

            foreach (ProviderRegistration registration in _providerRegistrations)
            {
                try
                {
                    if (registration.ShouldDispose)
                    {
                        registration.Provider.Dispose();
                    }
                }
#pragma warning disable CA1031
                catch
#pragma warning restore CA1031
                {
                    // Swallow exceptions on dispose
                }
            }
        }
    }

    public ILogger CreateLogger(string categoryName)
    {
        _ = Throw.IfNull(categoryName);

        if (CheckDisposed())
        {
            throw new ObjectDisposedException(nameof(LoggerFactory));
        }

        lock (_sync)
        {
            if (!_loggers.TryGetValue(categoryName, out ExtendedLogger? logger))
            {
                logger = new ExtendedLogger(this, CreateLoggers(categoryName));

                (logger.MessageLoggers, logger.ScopeLoggers) = ApplyFilters(logger.Loggers);

                _loggers[categoryName] = logger;
            }

            return logger;
        }
    }

    public void AddProvider(ILoggerProvider provider)
    {
        _ = Throw.IfNull(provider);

        if (CheckDisposed())
        {
            throw new ObjectDisposedException(nameof(LoggerFactory));
        }

        lock (_sync)
        {
            AddProviderRegistration(provider, dispose: true);

            foreach (KeyValuePair<string, ExtendedLogger> existingLogger in _loggers)
            {
                ExtendedLogger logger = existingLogger.Value;
                LoggerInformation[] loggerInformation = logger.Loggers;

                int newLoggerIndex = loggerInformation.Length;
                Array.Resize(ref loggerInformation, loggerInformation.Length + 1);
                loggerInformation[newLoggerIndex] = new LoggerInformation(provider, existingLogger.Key);

                logger.Loggers = loggerInformation;
                (logger.MessageLoggers, logger.ScopeLoggers) = ApplyFilters(logger.Loggers);
            }
        }
    }

    [MemberNotNull(nameof(_filterOptions))]
    private void RefreshFilters(LoggerFilterOptions filterOptions)
    {
        lock (_sync)
        {
            _filterOptions = filterOptions;
            foreach (KeyValuePair<string, ExtendedLogger> registeredLogger in _loggers)
            {
                ExtendedLogger logger = registeredLogger.Value;
                (logger.MessageLoggers, logger.ScopeLoggers) = ApplyFilters(logger.Loggers);
            }
        }
    }

    private void AddProviderRegistration(ILoggerProvider provider, bool dispose)
    {
        _providerRegistrations.Add(new ProviderRegistration
        {
            Provider = provider,
            ShouldDispose = dispose
        });

        if (provider is ISupportExternalScope supportsExternalScope)
        {
            _scopeProvider ??= new LoggerFactoryScopeProvider(_factoryOptions.ActivityTrackingOptions);

            supportsExternalScope.SetScopeProvider(_scopeProvider);
        }
    }

    private LoggerInformation[] CreateLoggers(string categoryName)
    {
        var loggers = new LoggerInformation[_providerRegistrations.Count];
        for (int i = 0; i < _providerRegistrations.Count; i++)
        {
            loggers[i] = new LoggerInformation(_providerRegistrations[i].Provider, categoryName);
        }

        return loggers;
    }

    private (MessageLogger[] messageLoggers, ScopeLogger[] scopeLoggers) ApplyFilters(LoggerInformation[] loggers)
    {
        var messageLoggers = new List<MessageLogger>();
        List<ScopeLogger>? scopeLoggers = _filterOptions.CaptureScopes ? [] : null;

        foreach (LoggerInformation loggerInformation in loggers)
        {
            LoggerRuleSelector.Select(_filterOptions,
                loggerInformation.ProviderType,
                loggerInformation.Category,
                out LogLevel? minLevel,
                out Func<string?, string?, LogLevel, bool>? filter);

            if (minLevel is > LogLevel.Critical)
            {
                continue;
            }

            messageLoggers.Add(new MessageLogger(loggerInformation.Logger, loggerInformation.Category, loggerInformation.ProviderType.FullName, minLevel, filter));

            if (!loggerInformation.ExternalScope)
            {
                scopeLoggers?.Add(new ScopeLogger(logger: loggerInformation.Logger, externalScopeProvider: null));
            }
        }

        if (_scopeProvider != null)
        {
            scopeLoggers?.Add(new ScopeLogger(logger: null, externalScopeProvider: _scopeProvider));
        }

        return (messageLoggers.ToArray(), scopeLoggers?.ToArray() ?? Array.Empty<ScopeLogger>());
    }

    private bool CheckDisposed() => _disposed;

    /// <summary>
    /// Gets the current config state that loggers should use.
    /// </summary>
    /// <remarks>
    /// This gets replaced whenever option monitors trigger. The loggers should sample this value
    /// and use it for an entire call to ILogger.Log so as to get a consistent view of config for the
    /// execution span of the function.
    /// </remarks>
    internal LoggerConfig Config { get; private set; }

    private LoggerConfig ComputeConfig(LoggerEnrichmentOptions? enrichmentOptions, LoggerRedactionOptions? redactionOptions)
    {
        if (enrichmentOptions == null)
        {
            enrichmentOptions = new LoggerEnrichmentOptions
            {
                CaptureStackTraces = Config.CaptureStackTraces,
                UseFileInfoForStackTraces = Config.UseFileInfoForStackTraces,
                IncludeExceptionMessageInStackTraces = Config.IncludeExceptionMessageInStackTraces,
                MaxStackTraceLength = Config.MaxStackTraceLength,
            };
        }

        if (redactionOptions == null)
        {
            redactionOptions = new LoggerRedactionOptions
            {
                ApplyDiscriminator = Config.AddRedactionDiscriminator,
            };
        }

        return new(_staticTags,
                _enrichers,
                enrichmentOptions.CaptureStackTraces,
                enrichmentOptions.UseFileInfoForStackTraces,
                enrichmentOptions.IncludeExceptionMessage,
                enrichmentOptions.MaxStackTraceLength,
                _redactorProvider,
                redactionOptions.ApplyDiscriminator);
    }

    private void UpdateEnrichmentOptions(LoggerEnrichmentOptions enrichmentOptions) => Config = ComputeConfig(enrichmentOptions, null);
    private void UpdateRedactionOptions(LoggerRedactionOptions redactionOptions) => Config = ComputeConfig(null, redactionOptions);

    private struct ProviderRegistration
    {
        public ILoggerProvider Provider;
        public bool ShouldDispose;
    }
}

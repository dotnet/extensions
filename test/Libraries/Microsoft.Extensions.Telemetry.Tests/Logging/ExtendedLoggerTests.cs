﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Logging.Test.Log;

public static class ExtendedLoggerTests
{
    [Fact]
    public static void Basic()
    {
        const string Category = "C1";

        using var provider = new Provider();
        var enricher = new ForcedEnricher(
            new[]
            {
                new KeyValuePair<string, object?>("EK1", "EV1"),
            });

        var staticEnricher = new ForcedEnricher(
            new[]
            {
                new KeyValuePair<string, object?>("SEK1", "SEV1"),
            });

        var redactorProvider = new FakeRedactorProvider(new FakeRedactorOptions
        {
            RedactionFormat = "REDACTED<{0}>",
        });

        using var lf = new ExtendedLoggerFactory(
            providers: new[] { provider },
            filterOptions: new StaticOptionsMonitor<LoggerFilterOptions>(new()),
            enrichmentOptions: new StaticOptionsMonitor<LoggerEnrichmentOptions>(new()),
            redactionOptions: new StaticOptionsMonitor<LoggerRedactionOptions>(new()),
            enrichers: new[] { enricher },
            staticEnrichers: new[] { staticEnricher },
            redactorProvider: redactorProvider,
            scopeProvider: null,
            factoryOptions: null);

        var logger = lf.CreateLogger(Category);
        logger.LogWarning("MSG0");

        var lmh = LogMethodHelper.GetHelper();
        lmh.Add("PK1", "PV1");
        logger.Log(LogLevel.Error, new EventId(1, "ID1"), lmh, null, (_, _) => "MSG1");

        var lms = LoggerMessageHelper.ThreadLocalState;
        var index = lms.ReserveTagSpace(1);
        lms.TagArray[index] = new("PK2", "PV2");

        index = lms.ReserveClassifiedTagSpace(2);
        lms.ClassifiedTagArray[index] = new("PK3", "PV3", SimpleClassifications.PrivateData);
        lms.ClassifiedTagArray[index + 1] = new("PK4", null, SimpleClassifications.PrivateData);

        logger.Log(LogLevel.Warning, new EventId(2, "ID2"), lms, null, (_, _) => "MSG2");

        var sink = provider.Logger!;
        var collector = sink.Collector;
        Assert.Equal(Category, sink.Category);
        Assert.Equal(3, collector.Count);

        var snap = collector.GetSnapshot();

        Assert.Equal(Category, snap[0].Category);
        Assert.Null(snap[0].Exception);
        Assert.Equal(new EventId(0), snap[0].Id);
        Assert.Equal("MSG0", snap[0].Message);
        Assert.Equal("EV1", snap[0].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[0].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[1].Category);
        Assert.Null(snap[1].Exception);
        Assert.Equal(new EventId(1, "ID1"), snap[1].Id);
        Assert.Equal("MSG1", snap[1].Message);
        Assert.Equal("PV1", snap[1].StructuredState!.GetValue("PK1"));
        Assert.Equal("EV1", snap[1].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[1].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[2].Category);
        Assert.Null(snap[2].Exception);
        Assert.Equal(new EventId(2, "ID2"), snap[2].Id);
        Assert.Equal("MSG2", snap[2].Message);
        Assert.Equal("PV2", snap[2].StructuredState!.GetValue("PK2"));
        Assert.Equal("REDACTED<PV3>", snap[2].StructuredState!.GetValue("PK3"));
        Assert.Null(snap[2].StructuredState!.GetValue("PK4"));
        Assert.Equal("EV1", snap[2].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[2].StructuredState!.GetValue("SEK1"));
    }

    [Theory]
    [CombinatorialData]
    public static void BagAndJoiner(bool objectVersion)
    {
        const string Category = "C1";

        using var provider = new Provider();
        var enricher = new FancyEnricher(
            new[]
            {
                new KeyValuePair<string, object?>("EK1", "EV1"),
                new KeyValuePair<string, object?>("EK2", "EV2"),
            }, objectVersion);

        using var lf = new ExtendedLoggerFactory(
            providers: new[] { provider },
            filterOptions: new StaticOptionsMonitor<LoggerFilterOptions>(new()),
            enrichmentOptions: new StaticOptionsMonitor<LoggerEnrichmentOptions>(new()),
            redactionOptions: new StaticOptionsMonitor<LoggerRedactionOptions>(new()),
            enrichers: new[] { enricher },
            staticEnrichers: Array.Empty<IStaticLogEnricher>(),
            redactorProvider: null,
            scopeProvider: null,
            factoryOptions: null);

        var logger = lf.CreateLogger(Category);
        logger.LogWarning("MSG0");

        var lmh = LogMethodHelper.GetHelper();
        lmh.Add("PK1", "PV1");
        logger.Log(LogLevel.Error, new EventId(1, "ID1"), lmh, null, (_, _) => "MSG1");

        var lms = LoggerMessageHelper.ThreadLocalState;
        var index = lms.ReserveTagSpace(1);
        lms.TagArray[index] = new("PK2", "PV2");
        logger.Log(LogLevel.Warning, new EventId(2, "ID2"), lms, null, (_, _) => "MSG2");

        var sink = provider.Logger!;
        var collector = sink.Collector;
        Assert.Equal(Category, sink.Category);
        Assert.Equal(3, collector.Count);

        var snap = collector.GetSnapshot();

        Assert.Equal(Category, snap[0].Category);
        Assert.Null(snap[0].Exception);
        Assert.Equal(new EventId(0), snap[0].Id);
        Assert.Equal("MSG0", snap[0].Message);
        Assert.Equal("EV1", snap[0].StructuredState!.GetValue("EK1"));
        Assert.Equal("EV2", snap[0].StructuredState!.GetValue("EK2"));

        Assert.Equal(Category, snap[1].Category);
        Assert.Null(snap[1].Exception);
        Assert.Equal(new EventId(1, "ID1"), snap[1].Id);
        Assert.Equal("MSG1", snap[1].Message);
        Assert.Equal("PV1", snap[1].StructuredState!.GetValue("PK1"));
        Assert.Equal("EV1", snap[1].StructuredState!.GetValue("EK1"));
        Assert.Equal("EV2", snap[0].StructuredState!.GetValue("EK2"));

        Assert.Equal(Category, snap[2].Category);
        Assert.Null(snap[2].Exception);
        Assert.Equal(new EventId(2, "ID2"), snap[2].Id);
        Assert.Equal("MSG2", snap[2].Message);
        Assert.Equal("PV2", snap[2].StructuredState!.GetValue("PK2"));
        Assert.Equal("EV1", snap[2].StructuredState!.GetValue("EK1"));
        Assert.Equal("EV2", snap[0].StructuredState!.GetValue("EK2"));
    }

    [Fact]
    public static void NullStateObject()
    {
        const string Category = "C1";

        using var provider = new Provider();
        var enricher = new ForcedEnricher(
            new[]
            {
                new KeyValuePair<string, object?>("EK1", "EV1"),
            });

        var staticEnricher = new ForcedEnricher(
            new[]
            {
                new KeyValuePair<string, object?>("SEK1", "SEV1"),
            });

        var redactorProvider = new FakeRedactorProvider(new FakeRedactorOptions
        {
            RedactionFormat = "REDACTED<{0}>",
        });

        using var lf = new ExtendedLoggerFactory(
            providers: new[] { provider },
            filterOptions: new StaticOptionsMonitor<LoggerFilterOptions>(new()),
            enrichmentOptions: new StaticOptionsMonitor<LoggerEnrichmentOptions>(new()),
            redactionOptions: new StaticOptionsMonitor<LoggerRedactionOptions>(new()),
            enrichers: new[] { enricher },
            staticEnrichers: new[] { staticEnricher },
            redactorProvider: redactorProvider,
            scopeProvider: null,
            factoryOptions: null);

        var logger = lf.CreateLogger(Category);
        logger.Log<object?>(LogLevel.Error, new EventId(0, "ID0"), null, null, (_, _) => "MSG0");
        logger.Log<object?>(LogLevel.Error, new EventId(0, "ID0b"), null, null, (_, _) => "MSG0b");

        var lmh = LogMethodHelper.GetHelper();
        logger.Log(LogLevel.Error, new EventId(1, "ID1"), (LogMethodHelper?)null, null, (_, _) => "MSG1");
        logger.Log(LogLevel.Error, new EventId(1, "ID1b"), (LogMethodHelper?)null, null, (_, _) => "MSG1b");

        var lms = LoggerMessageHelper.ThreadLocalState;
        logger.Log(LogLevel.Warning, new EventId(2, "ID2"), (LoggerMessageState?)null, null, (_, _) => "MSG2");
        logger.Log(LogLevel.Warning, new EventId(2, "ID2b"), (LoggerMessageState?)null, null, (_, _) => "MSG2b");

        var sink = provider.Logger!;
        var collector = sink.Collector;
        Assert.Equal(Category, sink.Category);
        Assert.Equal(6, collector.Count);

        var snap = collector.GetSnapshot();

        Assert.Equal(Category, snap[0].Category);
        Assert.Null(snap[0].Exception);
        Assert.Equal(new EventId(0, "ID0"), snap[0].Id);
        Assert.Equal("MSG0", snap[0].Message);
        Assert.Equal("EV1", snap[0].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[0].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[1].Category);
        Assert.Null(snap[1].Exception);
        Assert.Equal(new EventId(0, "ID0b"), snap[1].Id);
        Assert.Equal("MSG0b", snap[1].Message);
        Assert.Equal("EV1", snap[1].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[1].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[2].Category);
        Assert.Null(snap[2].Exception);
        Assert.Equal(new EventId(1, "ID1"), snap[2].Id);
        Assert.Equal("MSG1", snap[2].Message);
        Assert.Equal("EV1", snap[2].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[2].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[3].Category);
        Assert.Null(snap[3].Exception);
        Assert.Equal(new EventId(1, "ID1b"), snap[3].Id);
        Assert.Equal("MSG1b", snap[3].Message);
        Assert.Equal("EV1", snap[3].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[3].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[4].Category);
        Assert.Null(snap[4].Exception);
        Assert.Equal(new EventId(2, "ID2"), snap[4].Id);
        Assert.Equal("MSG2", snap[4].Message);
        Assert.Equal("EV1", snap[4].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[4].StructuredState!.GetValue("SEK1"));

        Assert.Equal(Category, snap[5].Category);
        Assert.Null(snap[5].Exception);
        Assert.Equal(new EventId(2, "ID2b"), snap[5].Id);
        Assert.Equal("MSG2b", snap[5].Message);
        Assert.Equal("EV1", snap[5].StructuredState!.GetValue("EK1"));
        Assert.Equal("SEV1", snap[5].StructuredState!.GetValue("SEK1"));
    }

    [Fact]
    public static void EnumerableStateObject()
    {
        const string Category = "C1";

        using var provider = new Provider();
        using var lf = new ExtendedLoggerFactory(
            providers: new[] { provider },
            filterOptions: new StaticOptionsMonitor<LoggerFilterOptions>(new()),
            enrichmentOptions: new StaticOptionsMonitor<LoggerEnrichmentOptions>(new()),
            redactionOptions: new StaticOptionsMonitor<LoggerRedactionOptions>(new()),
            enrichers: Array.Empty<ILogEnricher>(),
            staticEnrichers: Array.Empty<IStaticLogEnricher>(),
            redactorProvider: null,
            scopeProvider: null,
            factoryOptions: null);

        var logger = lf.CreateLogger(Category);

        var a = new[] { new KeyValuePair<string, object?>("K1", "V1") };
        var e = a.Where(_ => true);

        logger.Log(LogLevel.Warning, new EventId(0, "ID0"), e, null, (_, _) => "MSG0");

        var sink = provider.Logger!;
        var collector = sink.Collector;
        Assert.Equal(Category, sink.Category);
        Assert.Equal(1, collector.Count);

        var snap = collector.GetSnapshot();

        Assert.Equal(Category, snap[0].Category);
        Assert.Null(snap[0].Exception);
        Assert.Equal(new EventId(0, "ID0"), snap[0].Id);
        Assert.Equal("MSG0", snap[0].Message);
        Assert.Equal("V1", snap[0].StructuredState!.GetValue("K1"));
    }

    [Fact]
    public static void Filtering()
    {
        const string FilteredCategory = "C1";
        const string UnfilteredCategory = "C2";

        var filterOptions = new LoggerFilterOptions();
        filterOptions.Rules.Add(new LoggerFilterRule(null, FilteredCategory, null, (_, _, _) => false));

        using var provider = new Provider();
        using var lf = new ExtendedLoggerFactory(
            providers: new[] { provider },
            filterOptions: new StaticOptionsMonitor<LoggerFilterOptions>(filterOptions),
            enrichmentOptions: new StaticOptionsMonitor<LoggerEnrichmentOptions>(new()),
            redactionOptions: new StaticOptionsMonitor<LoggerRedactionOptions>(new()),
            enrichers: Array.Empty<ILogEnricher>(),
            staticEnrichers: Array.Empty<IStaticLogEnricher>(),
            redactorProvider: null,
            scopeProvider: null,
            factoryOptions: null);

        var filteredLogger = lf.CreateLogger(FilteredCategory);
        var unfilteredLogger = lf.CreateLogger(UnfilteredCategory);

        Assert.False(filteredLogger.IsEnabled(LogLevel.Warning));
        Assert.True(unfilteredLogger.IsEnabled(LogLevel.Warning));

        var fake = provider.Logger!;
        fake.ControlLevel(LogLevel.Warning, false);

        Assert.False(filteredLogger.IsEnabled(LogLevel.Warning));
        Assert.False(unfilteredLogger.IsEnabled(LogLevel.Warning));
    }

    [Fact]
    public static void StringStateObject()
    {
        const string Category = "C1";

        using var provider = new Provider();
        using var lf = new ExtendedLoggerFactory(
            providers: new[] { provider },
            filterOptions: new StaticOptionsMonitor<LoggerFilterOptions>(new()),
            enrichmentOptions: new StaticOptionsMonitor<LoggerEnrichmentOptions>(new()),
            redactionOptions: new StaticOptionsMonitor<LoggerRedactionOptions>(new()),
            enrichers: Array.Empty<ILogEnricher>(),
            staticEnrichers: Array.Empty<IStaticLogEnricher>(),
            redactorProvider: null,
            scopeProvider: null,
            factoryOptions: null);

        var logger = lf.CreateLogger(Category);

        logger.Log(LogLevel.Warning, new EventId(0, "ID0"), "PAYLOAD", null, (_, _) => "MSG0");

        var sink = provider.Logger!;
        var collector = sink.Collector;
        Assert.Equal(Category, sink.Category);
        Assert.Equal(1, collector.Count);

        var snap = collector.GetSnapshot();

        Assert.Equal(Category, snap[0].Category);
        Assert.Null(snap[0].Exception);
        Assert.Equal(new EventId(0, "ID0"), snap[0].Id);
        Assert.Equal("MSG0", snap[0].Message);
        Assert.Equal("PAYLOAD", snap[0].StructuredState!.GetValue("{OriginalFormat}"));
    }

    [Fact]
    public static void Exceptions()
    {
        const string Category = "C1";

        using var provider = new Provider();
        var stackTraceOptions = new LoggerEnrichmentOptions
        {
            CaptureStackTraces = true,
            UseFileInfoForStackTraces = true,
            MaxStackTraceLength = 4096,
        };

        using var lf = new ExtendedLoggerFactory(
            providers: new[] { provider },
            filterOptions: new StaticOptionsMonitor<LoggerFilterOptions>(new()),
            enrichmentOptions: new StaticOptionsMonitor<LoggerEnrichmentOptions>(stackTraceOptions),
            redactionOptions: new StaticOptionsMonitor<LoggerRedactionOptions>(new()),
            enrichers: Array.Empty<ILogEnricher>(),
            staticEnrichers: Array.Empty<IStaticLogEnricher>(),
            redactorProvider: new FakeRedactorProvider(),
            scopeProvider: null,
            factoryOptions: null);

        Exception ex;
        try
        {
            List<Exception> exceptions = new();
            try
            {
                throw new ArgumentNullException("ARG1");
            }
            catch (ArgumentNullException e)
            {
                exceptions.Add(e);
            }

            try
            {
                throw new ArgumentOutOfRangeException("ARG2");
            }
            catch (ArgumentOutOfRangeException e)
            {
                exceptions.Add(e);
            }

            try
            {
                throw new InvalidOperationException("Doing crazy things");
            }
            catch (InvalidOperationException e)
            {
                exceptions.Add(e);
            }

            throw new AggregateException("Outer", exceptions);
        }
        catch (AggregateException e)
        {
            ex = e;
        }

        var logger = lf.CreateLogger(Category);
        logger.Log<object?>(LogLevel.Error, new EventId(0, "ID0"), null, null, (_, _) => "MSG0");
        logger.Log<object?>(LogLevel.Error, new EventId(0, "ID0b"), null, ex, (_, _) => "MSG0b");

        var lmh = LogMethodHelper.GetHelper();
        logger.Log(LogLevel.Error, new EventId(1, "ID1"), lmh, null, (_, _) => "MSG1");
        logger.Log(LogLevel.Error, new EventId(1, "ID1b"), lmh, ex, (_, _) => "MSG1b");

        var lms = LoggerMessageHelper.ThreadLocalState;
        logger.Log(LogLevel.Warning, new EventId(2, "ID2"), lms, null, (_, _) => "MSG2");
        logger.Log(LogLevel.Warning, new EventId(2, "ID2b"), lms, ex, (_, _) => "MSG2b");

        var sink = provider.Logger!;
        var collector = sink.Collector;
        Assert.Equal(Category, sink.Category);
        Assert.Equal(6, collector.Count);

        var snap = collector.GetSnapshot();

        Assert.Equal(Category, snap[0].Category);
        Assert.Null(snap[0].Exception);
        Assert.Equal(new EventId(0, "ID0"), snap[0].Id);
        Assert.Equal("MSG0", snap[0].Message);

        Assert.Equal(Category, snap[1].Category);
        Assert.NotNull(snap[1].Exception);
        Assert.Equal(new EventId(0, "ID0b"), snap[1].Id);
        Assert.Equal("MSG0b", snap[1].Message);

        Assert.Equal(Category, snap[2].Category);
        Assert.Null(snap[2].Exception);
        Assert.Equal(new EventId(1, "ID1"), snap[2].Id);
        Assert.Equal("MSG1", snap[2].Message);

        Assert.Equal(Category, snap[3].Category);
        Assert.NotNull(snap[3].Exception);
        Assert.Equal(new EventId(1, "ID1b"), snap[3].Id);
        Assert.Equal("MSG1b", snap[3].Message);

        Assert.Equal(Category, snap[4].Category);
        Assert.Null(snap[4].Exception);
        Assert.Equal(new EventId(2, "ID2"), snap[4].Id);
        Assert.Equal("MSG2", snap[4].Message);

        Assert.Equal(Category, snap[5].Category);
        Assert.NotNull(snap[5].Exception);
        Assert.Equal(new EventId(2, "ID2b"), snap[5].Id);
        Assert.Equal("MSG2b", snap[5].Message);

        var stackTrace = snap[5].StructuredState!.GetValue("stackTrace")!;
        Assert.Contains("AggregateException", stackTrace);
        Assert.Contains("ArgumentNullException", stackTrace);
        Assert.Contains("ArgumentOutOfRangeException", stackTrace);
        Assert.Contains("InvalidOperationException", stackTrace);
    }

#if false
    [Fact]
    public void Log_IgnoresExceptionInIntermediateLoggersAndThrowsAggregateException()
    {
        // Arrange
        var store = new List<string>();
        var loggerFactory = TestLoggerBuilder.Create(builder => builder
            .AddProvider(new CustomLoggerProvider("provider1", ThrowExceptionAt.None, store))
            .AddProvider(new CustomLoggerProvider("provider2", ThrowExceptionAt.Log, store))
            .AddProvider(new CustomLoggerProvider("provider3", ThrowExceptionAt.None, store)));

        var logger = loggerFactory.CreateLogger("Test");

        // Act
        var aggregateException = Assert.Throws<AggregateException>(() => logger.LogInformation("Hello!"));

        // Assert
        Assert.Equal(new[] { "provider1.Test-Hello!", "provider3.Test-Hello!" }, store);
        Assert.NotNull(aggregateException);
        Assert.StartsWith("An error occurred while writing to logger(s).", aggregateException.Message);
        Assert.Single(aggregateException.InnerExceptions);
        var exception = aggregateException.InnerExceptions[0];
        Assert.Equal("provider2.Test-Error occurred while logging data.", exception.Message);
    }

    [Fact]
    public static void BeginScope_IgnoresExceptionInIntermediateLoggersAndThrowsAggregateException()
    {
        // Arrange
        var store = new List<string>();
        var loggerFactory = TestLoggerBuilder.Create(builder => builder
            .AddProvider(new CustomLoggerProvider("provider1", ThrowExceptionAt.None, store))
            .AddProvider(new CustomLoggerProvider("provider2", ThrowExceptionAt.BeginScope, store))
            .AddProvider(new CustomLoggerProvider("provider3", ThrowExceptionAt.None, store)));

        var logger = loggerFactory.CreateLogger("Test");

        // Act
        var aggregateException = Assert.Throws<AggregateException>(() => logger.BeginScope("Scope1"));

        // Assert
        Assert.Equal(new[] { "provider1.Test-Scope1", "provider3.Test-Scope1" }, store);
        Assert.NotNull(aggregateException);
        Assert.StartsWith("An error occurred while writing to logger(s).", aggregateException.Message);
        Assert.Single(aggregateException.InnerExceptions);
        var exception = aggregateException.InnerExceptions[0];
        Assert.Equal("provider2.Test-Error occurred while creating scope.", exception.Message);
    }

    [Fact]
    public static void IsEnabled_IgnoresExceptionInIntermediateLoggers()
    {
        // Arrange
        var store = new List<string>();
        var loggerFactory = TestLoggerBuilder.Create(builder => builder
            .AddProvider(new CustomLoggerProvider("provider1", ThrowExceptionAt.None, store))
            .AddProvider(new CustomLoggerProvider("provider2", ThrowExceptionAt.IsEnabled, store))
            .AddProvider(new CustomLoggerProvider("provider3", ThrowExceptionAt.None, store)));

        var logger = loggerFactory.CreateLogger("Test");

        // Act
        var aggregateException = Assert.Throws<AggregateException>(() => logger.LogInformation("Hello!"));

        // Assert
        Assert.Equal(new[] { "provider1.Test-Hello!", "provider3.Test-Hello!" }, store);
        Assert.NotNull(aggregateException);
        Assert.StartsWith("An error occurred while writing to logger(s).", aggregateException.Message);
        Assert.Single(aggregateException.InnerExceptions);
        var exception = aggregateException.InnerExceptions[0];
        Assert.Equal("provider2.Test-Error occurred while checking if logger is enabled.", exception.Message);
    }

    [Fact]
    public static void Log_AggregatesExceptionsFromMultipleLoggers()
    {
        // Arrange
        var store = new List<string>();
        var loggerFactory = TestLoggerBuilder.Create(builder => builder
            .AddProvider(new CustomLoggerProvider("provider1", ThrowExceptionAt.Log, store))
            .AddProvider(new CustomLoggerProvider("provider2", ThrowExceptionAt.Log, store)));

        var logger = loggerFactory.CreateLogger("Test");

        // Act
        var aggregateException = Assert.Throws<AggregateException>(() => logger.LogInformation("Hello!"));

        // Assert
        Assert.Empty(store);
        Assert.NotNull(aggregateException);
        Assert.StartsWith("An error occurred while writing to logger(s).", aggregateException.Message);
        var exceptions = aggregateException.InnerExceptions;
        Assert.Equal(2, exceptions.Count);
        Assert.Equal("provider1.Test-Error occurred while logging data.", exceptions[0].Message);
        Assert.Equal("provider2.Test-Error occurred while logging data.", exceptions[1].Message);
    }
#endif

    [Fact]
    public static void LoggerCanGetProviderAfterItIsCreated()
    {
        // Arrange
        var store = new List<string>();
        using var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger("Test");
        using var provider = new CustomLoggerProvider("provider1", ThrowExceptionAt.None, store);

        loggerFactory.AddProvider(provider);

        // Act
        logger.LogInformation("Hello");

        // Assert
        Assert.Equal(new[] { "provider1.Test-Hello" }, store);
    }

    [Fact]
    public static void ScopesAreNotCreatedForDisabledLoggers()
    {
        var provider = new Mock<ILoggerProvider>();
        var logger = new Mock<ILogger>();

        provider.Setup(loggerProvider => loggerProvider.CreateLogger(It.IsAny<string>()))
            .Returns(logger.Object);

        using var factory = Utils.CreateLoggerFactory(
            builder =>
            {
                builder.AddProvider(provider.Object);

                // Disable all logs
                builder.AddFilter(null, LogLevel.None);
            });

        var newLogger = factory.CreateLogger("Logger");
        using (newLogger.BeginScope("Scope"))
        {
            // nop
        }

        provider.Verify(p => p.CreateLogger("Logger"), Times.Once);
        logger.Verify(l => l.BeginScope(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public static void ScopesAreNotCreatedWhenScopesAreDisabled()
    {
        var provider = new Mock<ILoggerProvider>();
        var logger = new Mock<ILogger>();

        provider.Setup(loggerProvider => loggerProvider.CreateLogger(It.IsAny<string>()))
            .Returns(logger.Object);

        using var factory = Utils.CreateLoggerFactory(
            builder =>
            {
                builder.AddProvider(provider.Object);
                builder.Services.Configure<LoggerFilterOptions>(options => options.CaptureScopes = false);
            });

        var newLogger = factory.CreateLogger("Logger");
        using (newLogger.BeginScope("Scope"))
        {
            // nop
        }

        provider.Verify(p => p.CreateLogger("Logger"), Times.Once);
        logger.Verify(l => l.BeginScope(It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public static void ScopesAreNotCreatedInIScopeProviderWhenScopesAreDisabled()
    {
        var provider = new Mock<ILoggerProvider>();
        var logger = new Mock<ILogger>();

        IExternalScopeProvider? externalScopeProvider = null;

        provider.Setup(loggerProvider => loggerProvider.CreateLogger(It.IsAny<string>()))
            .Returns(logger.Object);
        provider.As<ISupportExternalScope>().Setup(scope => scope.SetScopeProvider(It.IsAny<IExternalScopeProvider>()))
            .Callback((IExternalScopeProvider scopeProvider) => externalScopeProvider = scopeProvider);

        using var factory = Utils.CreateLoggerFactory(
            builder =>
            {
                builder.AddProvider(provider.Object);
                builder.Services.Configure<LoggerFilterOptions>(options => options.CaptureScopes = false);
            });

        var newLogger = factory.CreateLogger("Logger");
        int scopeCount = 0;

        using (newLogger.BeginScope("Scope"))
        {
            externalScopeProvider!.ForEachScope<object>((_, _) => scopeCount++, null!);
        }

        provider.Verify(p => p.CreateLogger("Logger"), Times.Once);
        logger.Verify(l => l.BeginScope(It.IsAny<object>()), Times.Never);
        Assert.Equal(0, scopeCount);
    }

    [Fact]
    public static void CaptureScopesIsReadFromConfiguration()
    {
        var provider = new Mock<ILoggerProvider>();
        var logger = new Mock<ILogger>();
        var json = @"{ ""CaptureScopes"": ""false"" }";

        using var config = TestConfiguration.Create(() => json);
        IExternalScopeProvider? externalScopeProvider = null;

        provider.Setup(loggerProvider => loggerProvider.CreateLogger(It.IsAny<string>()))
            .Returns(logger.Object);
        provider.As<ISupportExternalScope>().Setup(scope => scope.SetScopeProvider(It.IsAny<IExternalScopeProvider>()))
            .Callback((IExternalScopeProvider scopeProvider) => externalScopeProvider = scopeProvider);

        using var factory = Utils.CreateLoggerFactory(
            builder =>
            {
                builder.AddProvider(provider.Object);
                builder.AddConfiguration(config);
            });

        var newLogger = factory.CreateLogger("Logger");
        int scopeCount = 0;

        using (newLogger.BeginScope("Scope"))
        {
            externalScopeProvider!.ForEachScope<object>((_, _) => scopeCount++, null!);
            Assert.Equal(0, scopeCount);
        }

        json = @"{ ""CaptureScopes"": ""true"" }";
        config.Reload();

        scopeCount = 0;
        using (newLogger.BeginScope("Scope"))
        {
            externalScopeProvider.ForEachScope<object>((_, _) => scopeCount++, null!);
            Assert.Equal(1, scopeCount);
        }
    }

    private sealed class CustomLoggerProvider : ILoggerProvider
    {
        private readonly string _providerName;
        private readonly ThrowExceptionAt _throwExceptionAt;
        private readonly List<string> _store;

        public CustomLoggerProvider(string providerName, ThrowExceptionAt throwExceptionAt, List<string> store)
        {
            _providerName = providerName;
            _throwExceptionAt = throwExceptionAt;
            _store = store;
        }

        public ILogger CreateLogger(string name)
        {
            return new CustomLogger($"{_providerName}.{name}", _throwExceptionAt, _store);
        }

        public void Dispose()
        {
            // nop[
        }
    }

    private sealed class CustomLogger : ILogger
    {
        private readonly string _name;
        private readonly ThrowExceptionAt _throwExceptionAt;
        private readonly List<string> _store;

        public CustomLogger(string name, ThrowExceptionAt throwExceptionAt, List<string> store)
        {
            _name = name;
            _throwExceptionAt = throwExceptionAt;
            _store = store;
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            if (_throwExceptionAt == ThrowExceptionAt.BeginScope)
            {
                throw new InvalidOperationException($"{_name}-Error occurred while creating scope.");
            }

            _store.Add($"{_name}-{state}");

            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (_throwExceptionAt == ThrowExceptionAt.IsEnabled)
            {
                throw new InvalidOperationException($"{_name}-Error occurred while checking if logger is enabled.");
            }

            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (_throwExceptionAt == ThrowExceptionAt.Log)
            {
                throw new InvalidOperationException($"{_name}-Error occurred while logging data.");
            }

            _store.Add($"{_name}-{state}");
        }
    }

    private enum ThrowExceptionAt
    {
        None,
        BeginScope,
        Log,
        IsEnabled
    }

    private static string? GetValue(this IReadOnlyList<KeyValuePair<string, string>> state, string name)
    {
        foreach (var kvp in state)
        {
            if (kvp.Key == name)
            {
                return kvp.Value;
            }
        }

        return null;
    }

    private sealed class Provider : ILoggerProvider
    {
        public FakeLogger? Logger { get; private set; }

        public ILogger CreateLogger(string categoryName)
        {
            Logger = new FakeLogger((FakeLogCollector?)null, categoryName);
            return Logger;
        }

        public void Dispose()
        {
            // nothing to do
        }
    }

    private sealed class ForcedEnricher : ILogEnricher, IStaticLogEnricher
    {
        private readonly KeyValuePair<string, object?>[] _values;

        public ForcedEnricher(KeyValuePair<string, object?>[] values)
        {
            _values = values;
        }

        public void Enrich(IEnrichmentTagCollector enrichmentPropertyBag)
        {
            foreach (var kvp in _values)
            {
                enrichmentPropertyBag.Add(kvp.Key, kvp.Value!);
            }
        }
    }

    private sealed class FancyEnricher : ILogEnricher
    {
        private readonly KeyValuePair<string, object?>[] _values;
        private readonly bool _objectVersion;

        public FancyEnricher(KeyValuePair<string, object?>[] values, bool objectVersion)
        {
            _values = values;
            _objectVersion = objectVersion;
        }

        public void Enrich(IEnrichmentTagCollector collector)
        {
            if (_objectVersion)
            {
                var p = (KeyValuePair<string, object>[])(object)_values;
                foreach (var kvp in p)
                {
                    collector.Add(kvp.Key, kvp.Value);
                }
            }
            else
            {
                var a = new KeyValuePair<string, string>[_values.Length];
                int i = 0;
                foreach (var kvp in _values)
                {
                    a[i++] = new(kvp.Key, (string)kvp.Value!);
                }

                foreach (var kvp in a)
                {
                    collector.Add(kvp.Key, kvp.Value);
                }
            }
        }
    }

    private sealed class StaticOptionsMonitor<T> : IOptionsMonitor<T>
    {
        public StaticOptionsMonitor(T currentValue)
        {
            CurrentValue = currentValue;
        }

        public IDisposable? OnChange(Action<T, string> listener) => null;
        public T Get(string? name) => CurrentValue;
        public T CurrentValue { get; }
    }
}

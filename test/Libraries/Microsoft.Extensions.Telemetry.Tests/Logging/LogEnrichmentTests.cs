// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Enrichment;
using Microsoft.Extensions.Telemetry.Logging.Test.Internals;
using OpenTelemetry;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Logging.Test;

public sealed class LogEnrichmentTests
{
    [Fact]
    public void LogWithEmptyEnricher()
    {
        // Arrange
        using var exporter = new TestExporter();
        var logMessage = "This is testing {user}";

        var logger = Helpers.CreateLogger(builder =>
            builder.Services.AddLogEnricher<EmptyEnricher>(), exporter);
        var dictExpected = new Dictionary<string, object>
            {
                { "{OriginalFormat}", logMessage },
                { "user", "testUser" }
            };

        // Act
        logger.LogError(logMessage, "testUser");

        // Assert
        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
    }

    [Fact]
    public void LogWithSingleEnricher()
    {
        // Arrange
        using var exporter = new TestExporter();
        var logger = Helpers.CreateLogger(b =>
                    b.Services.AddLogEnricher<SimpleEnricher>(), exporter);
        var logMessage = "This is testing {user}";
        var dictExpected = new Dictionary<string, object>
            {
                { "{OriginalFormat}", logMessage },
                { "user", "testUser" },
                { "enriched test attribute key", "enriched test attribute value" },
            };

        // Act
        logger.LogError(logMessage, "testUser");

        // Assert
        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
    }

    [Fact]
    public void LogWithEmptyStringEnricher()
    {
        // Arrange
        using var exporter = new TestExporter();
        var logger = Helpers.CreateLogger(b =>
            b.Services.AddLogEnricher<EmptyStringEnricher>(), exporter);
        var logMessage = "This is testing {user}";
        var dictExpected = new Dictionary<string, object>
            {
                { "{OriginalFormat}", logMessage },
                { "user", "testUser" },
                { "key1", string.Empty }
            };

        // Act
        logger.LogError(logMessage, "testUser");

        // Assert
        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
    }

    [Fact]
    public void LogWithManyEnrichersCreatedAtSamePoint()
    {
        // Arrange
        using var exporter = new TestExporter();

        var hostBuilder = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(builder =>
            {
                _ = builder.Services.AddLogEnricher<SimpleEnricher>();
                _ = builder
                    .AddOpenTelemetryLogging()
                    .AddProcessor(new SimpleLogRecordExportProcessor(exporter))
                    .Services.AddLogEnricher<AnotherEnricher>();
            });

        using var host = hostBuilder.Build();
        var logger = host.Services.GetRequiredService<ILogger<LogEnrichmentTests>>();

        var logMessage = "This is testing {user}";
        var dictExpected = new Dictionary<string, object>
            {
                { "{OriginalFormat}", logMessage },
                { "user", "testUser" },
                { "enriched test attribute key", "enriched test attribute value" },
                { "another's key", "another's value" }
            };

        // Act
        logger.LogError(logMessage, "testUser");

        // Assert
        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
    }

    [Fact]
    public void LogWithManyEnrichersCreatedAtDifferentPoints()
    {
        // Arrange
        using var exporter = new TestExporter();

        var hostBuilder = FakeHost.CreateBuilder(options => options.FakeLogging = false)
            .ConfigureLogging(builder =>
            {
                _ = builder.Services.AddLogEnricher<SimpleEnricher>();
                _ = builder
                    .AddOpenTelemetryLogging()
                    .AddProcessor(new SimpleLogRecordExportProcessor(exporter))
                    .Services.AddLogEnricher<AnotherEnricher>();
            })
            .ConfigureLogging(builder => builder.Services
                .AddLogEnricher(new FlexibleEnricher("key1", "value"))
                .AddLogEnricher(new PrimitiveValuesEnricher("key2", 1)));

        using var host = hostBuilder.Build();
        var logger = host.Services.GetRequiredService<ILogger<LogEnrichmentTests>>();

        var logMessage = "This is testing {user}";
        var dictExpected = new Dictionary<string, object>
            {
                { "{OriginalFormat}", logMessage },
                { "user", "testUser" },
                { "enriched test attribute key", "enriched test attribute value" },
                { "another's key", "another's value" },
                { "key1", "value" },
                { "key2", 1 }
            };

        // Act
        logger.LogError(logMessage, "testUser");

        // Assert
        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
    }

    private class CustomState
    {
        public string? Property { get; set; }
    }

    [Fact]
    public void LogWithNullState()
    {
        // Arrange
        using var exporter = new TestExporter();
        var logger = Helpers.CreateLogger(b =>
            b.Services.AddLogEnricher<SimpleEnricher>(), exporter);
        var dictExpected = new Dictionary<string, object>
            {
                { "enriched test attribute key", "enriched test attribute value" }
            };
        CustomState? state = null;

        // Act
        logger.Log(LogLevel.Information, 1, state, null, (_, _) => " test formatter ");

        // Assert
        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData(null, null)]
    [InlineData("", "test")]
    [InlineData("", null)]
    public void LogWithInvalidValues_ThrowsException(string? key, string? value)
    {
        // Arrange
        using var exporter = new TestExporter();
        var logMessage = "This is testing {user}";
        var enricher = new FlexibleEnricher(key, value);
        var enricher2 = new PrimitiveValuesEnricher(key, 1);
        var logger = Helpers.CreateLogger(b =>
        {
            b.Services.AddLogEnricher(enricher);
            b.Services.AddLogEnricher(enricher2);
        }, exporter);

        // Assert
        Assert.Throws<AggregateException>(() => logger.Log(LogLevel.Information, logMessage));
    }

    [Fact]
    public void LogWithCustomState()
    {
        // Arrange
        using var exporter = new TestExporter();
        var logger = Helpers.CreateLogger(b =>
            b.Services.AddLogEnricher<SimpleEnricher>(), exporter);
        var state = new CustomState { Property = "custom state property" };
        var dictExpected = new Dictionary<string, object>
            {
                { "enriched test attribute key", "enriched test attribute value" },
                { "{OriginalFormat}", state }
            };

        // Act
        logger.Log(LogLevel.Information, 2, state, null, (_, _) => " test formatter ");

        // Assert
        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
    }

    [Fact]
    public void LogWithMultipleEnrichers()
    {
        // Arrange
        using var exporter = new TestExporter();

        var logger = Helpers.CreateLogger(b =>
            {
                b.Services.AddLogEnricher<SimpleEnricher>();
                b.Services.AddLogEnricher<AnotherEnricher>();
            }, exporter);
        var logMessage = "This is testing {user}";
        var dictExpected = new Dictionary<string, object>
            {
                { "{OriginalFormat}", logMessage },
                { "user", "testUser" },
                { "enriched test attribute key", "enriched test attribute value" },
                { "another's key", "another's value" }
            };

        // Act
        logger.LogError(logMessage, "testUser");

        // Assert
        Assert.True(Helpers.CompareStateValues(exporter.FirstState!, dictExpected));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Enrichment.Process.Test.Internals;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Enrichment.Process.Test;

public class ProcessLogEnricherTests
{
    private readonly int _processId = System.Diagnostics.Process.GetCurrentProcess().Id;

    [Fact]
    public void ProcessLogEnricher_GivenInvalidArguments_Throws()
    {
        // Arrange
        var optionsNull = new Mock<IOptions<ProcessLogEnricherOptions>>();
        optionsNull.Setup(o => o.Value).Returns<IOptions<ProcessLogEnricherOptions>>(null!);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new ProcessLogEnricher(optionsNull.Object));
    }

    [Fact]
    public void ProcessLogEnricherOptions_EnabledByDefault()
    {
        // Arrange & Act
        var options = new ProcessLogEnricherOptions();

        // Assert
        Assert.True(options.ProcessId);
        Assert.False(options.ThreadId);
    }

    [Fact]
    public void ProcessLogEnricher_GivenEnricherOptions_Enriches()
    {
        // Arrange
        var options = new ProcessLogEnricherOptions
        {
            ProcessId = true,
            ThreadId = true
        };

        var enricher = new ProcessLogEnricher(options.ToOptions());
        var staticEnricher = new StaticProcessLogEnricher(options.ToOptions());
        var enrichedProperties = new TestLogEnrichmentTagCollector(new List<KeyValuePair<string, object>>());

        // Act
        enricher.Enrich(enrichedProperties);
        staticEnricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Properties;

        // Assert
        if (options.ThreadId)
        {
            Assert.Equal(Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture), enrichedState[ProcessEnricherTagNames.ThreadId]);
        }

        if (options.ProcessId)
        {
            Assert.Equal(_processId.ToString(CultureInfo.InvariantCulture), enrichedState[ProcessEnricherTagNames.ProcessId]);
        }
    }

    [Fact]
    public void ProcessLogEnricher_GivenDisabledEnricherOptions_DoesNotEnrich()
    {
        // Arrange
        var options = new ProcessLogEnricherOptions
        {
            ProcessId = false,
            ThreadId = false
        };

        var enricher = new ProcessLogEnricher(options.ToOptions());
        var staticEnricher = new StaticProcessLogEnricher(options.ToOptions());
        var enrichedProperties = new TestLogEnrichmentTagCollector();

        // Act
        enricher.Enrich(enrichedProperties);
        staticEnricher.Enrich(enrichedProperties);
        var enrichedState = enrichedProperties.Properties;

        // Assert
        Assert.False(enrichedState.ContainsKey(ProcessEnricherTagNames.ProcessId));
        Assert.False(enrichedState.ContainsKey(ProcessEnricherTagNames.ThreadId));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Enrichment.Test;

public class EnricherExtensionsTests
{
    [Fact]
    public void CreateLoggerWithNullEnricher()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddLogEnricher(null!));
    }

    [Fact]
    public void EnrichmentLoggingBuilder_GivenNullArguments_Throws()
    {
        // Arrange
        var enricher = new EmptyEnricher();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddLogEnricher(enricher));

        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddLogEnricher(null!));
    }

    [Fact]
    public void ServiceCollection_GivenNullArguments_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddMetricEnricher<EmptyEnricher>());

        Assert.Throws<ArgumentNullException>(() =>
            ((IServiceCollection)null!).AddMetricEnricher(new EmptyEnricher()));

        Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection()
                .AddMetricEnricher(null!));
    }

    [Fact]
    public void ServiceCollection_AddMultipleMetricEnrichersSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddMetricEnricher<EmptyEnricher>();
        services.AddMetricEnricher(new TestEnricher());

        using var provider = services.BuildServiceProvider();
        var enrichersCollection = provider.GetServices<IMetricEnricher>();

        var enricherCount = 0;
        foreach (var enricher in enrichersCollection)
        {
            enricherCount++;
        }

        Assert.Equal(2, enricherCount);
    }

    [Fact]
    public void AddLogEnricher()
    {
        var services = new ServiceCollection();
        services.AddLogEnricher<EmptyEnricher>();
        services.AddLogEnricher(new TestEnricher());

        using var provider = services.BuildServiceProvider();
        var enrichersCollection = provider.GetServices<ILogEnricher>();

        var enricherCount = 0;
        foreach (var enricher in enrichersCollection)
        {
            enricherCount++;
        }

        Assert.Equal(2, enricherCount);
    }

    [Fact]
    public void AddStaticLogEnricher()
    {
        var services = new ServiceCollection();
        services.AddStaticLogEnricher<EmptyEnricher>();
        services.AddStaticLogEnricher(new TestEnricher());

        using var provider = services.BuildServiceProvider();
        var enrichersCollection = provider.GetServices<IStaticLogEnricher>();

        var enricherCount = 0;
        foreach (var enricher in enrichersCollection)
        {
            enricherCount++;
        }

        Assert.Equal(2, enricherCount);
    }

    internal class EmptyEnricher : IMetricEnricher, ILogEnricher, IStaticLogEnricher
    {
        public void Enrich(IEnrichmentPropertyBag enrichmentBag)
        {
            // intentionally left empty
        }
    }

    internal class TestEnricher : IMetricEnricher, ILogEnricher, IStaticLogEnricher
    {
        public void Enrich(IEnrichmentPropertyBag enrichmentBag)
        {
            enrichmentBag.Add("testKey", "testValue");
        }
    }
}

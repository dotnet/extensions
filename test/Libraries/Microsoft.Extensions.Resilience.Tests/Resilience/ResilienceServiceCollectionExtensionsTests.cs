// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Resilience.Internal;
using Moq;
using Polly.Telemetry;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test.Resilience;

public class ResilienceServiceCollectionExtensionsTests
{
    private readonly Mock<IExceptionSummarizer> _summarizer = new(MockBehavior.Strict);

    private IServiceCollection _services;

    public ResilienceServiceCollectionExtensionsTests()
    {
        _services = new ServiceCollection().AddResilienceEnrichment();
        _services.TryAddSingleton(_summarizer.Object);
    }

    [Fact]
    public void AddResilienceEnrichment_EnsureMetricsEnricherRegistered()
    {
        var count = _services.Count;

        _services.AddResilienceEnrichment();

        var enrichers = _services.BuildServiceProvider().GetRequiredService<IOptions<TelemetryOptions>>().Value.MeteringEnrichers;
        enrichers.Should().HaveCount(1);
        enrichers.Single().Should().BeOfType<ResilienceMetricsEnricher>();
    }

    [Fact]
    public void AddResilienceEnrichment_Twice_NoNewServices()
    {
        var count = _services.Count;

        _services.AddResilienceEnrichment();

        _services.Count.Should().Be(count);
    }

    [Fact]
    public void ConfigureFailureResultContext_Ok()
    {
        var count = _services.Count;
        _services.ConfigureFailureResultContext<int>(_ => FailureResultContext.Create("dummy", "dummy", "dummy"));
        var factories = _services.BuildServiceProvider().GetRequiredService<IOptions<FailureEventMetricsOptions>>().Value.Factories;

        factories[typeof(int)](10).FailureReason.Should().Be("dummy");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Resilience.Resilience;
using Microsoft.Extensions.Telemetry.Testing.Metering;
using Moq;
using Polly;
using Polly.Registry;
using Polly.Telemetry;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test.Resilience;

#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize

public class ResilienceServiceCollectionExtensionsTests : IDisposable
{
    private readonly Mock<IExceptionSummarizer> _summarizer = new(MockBehavior.Strict);

    private ResilienceStrategyTelemetry? _telemetry;
    private MetricCollector<int> _metricCollector;
    private IServiceCollection _services;

    public ResilienceServiceCollectionExtensionsTests()
    {
        _metricCollector = new MetricCollector<int>(null, "Polly", "resilience-events");
        _services = new ServiceCollection()
            .AddResilienceEnrichment()
            .AddResilienceStrategy("dummy", builder =>
            {
                builder.AddStrategy(context =>
                {
                    _telemetry = context.Telemetry;
                    return Mock.Of<ResilienceStrategy>();
                },
                Mock.Of<ResilienceStrategyOptions>());
            });

        _services.TryAddSingleton(_summarizer.Object);
    }

    public void Dispose() => _metricCollector.Dispose();

    private IReadOnlyDictionary<string, object?> Tags => _metricCollector.LastMeasurement!.Tags;

    [Fact]
    public void AddResilienceEnrichment_NoOutcome_EnsureDimensions()
    {
        Build();
        _telemetry!.Report("dummy-event", ResilienceContext.Get(), string.Empty);

        Tags["failure-reason"].Should().BeNull();
        Tags["failure-source"].Should().BeNull();
        Tags["failure-summary"].Should().BeNull();
        Tags["dep-name"].Should().BeNull();
        Tags["req-name"].Should().BeNull();
    }

    [Fact]
    public void AddResilienceEnrichment_Twice_NoNewServices()
    {
        var count = _services.Count;

        _services.AddResilienceEnrichment();

        _services.Count.Should().Be(count);
    }

    [Fact]
    public void AddResilienceEnrichment_Exception_EnsureDimensions()
    {
        _summarizer.Setup(v => v.Summarize(It.IsAny<Exception>())).Returns(new ExceptionSummary("type", "desc", "details"));

        Build();
        _telemetry!.Report(
            "dummy-event",
            new OutcomeArguments<string, string>(ResilienceContext.Get(), Outcome.FromException<string>(new InvalidOperationException { Source = "my-source" }), string.Empty));

        Tags["failure-reason"].Should().Be("InvalidOperationException");
        Tags["failure-summary"].Should().Be("type:desc:details");
        Tags["failure-source"].Should().Be("my-source");
    }

    [Fact]
    public void AddResilienceEnrichment_Outcome_EnsureDimensions()
    {
        _services.ConfigureFailureResultContext<string>(v => FailureResultContext.Create("my-source", "my-reason", v));

        Build();
        _telemetry!.Report(
            "dummy-event",
            new OutcomeArguments<string, string>(ResilienceContext.Get(), Outcome.FromResult("string-result"), string.Empty));

        Tags["failure-source"].Should().Be("my-source");
        Tags["failure-reason"].Should().Be("my-reason");
        Tags["failure-summary"].Should().Be("string-result");
    }

    [Fact]
    public void AddResilienceEnrichment_RequestMetadata_EnsureDimensions()
    {
        var context = ResilienceContext.Get();
        context.SetRequestMetadata(new RequestMetadata { RequestName = "my-req", DependencyName = "my-dep" });

        Build();
        _telemetry!.Report("dummy-event", context, string.Empty);
        var tags = _metricCollector.LastMeasurement!.Tags;

        Tags["dep-name"].Should().Be("my-dep");
        tags["req-name"].Should().Be("my-req");
    }

    [Fact]
    public void AddResilienceEnrichment_RequestMetadataFromOutgoingRequestContext_EnsureDimensions()
    {
        var requestMetadata = new RequestMetadata { RequestName = "my-req", DependencyName = "my-dep" };
        _services.TryAddSingleton(Mock.Of<IOutgoingRequestContext>(v => v.RequestMetadata == requestMetadata));

        Build();
        _telemetry!.Report("dummy-event", ResilienceContext.Get(), string.Empty);

        Tags["dep-name"].Should().Be("my-dep");
        Tags["req-name"].Should().Be("my-req");
    }

    private void Build()
    {
        _services.BuildServiceProvider().GetRequiredService<ResilienceStrategyProvider<string>>().GetStrategy("dummy");
    }
}

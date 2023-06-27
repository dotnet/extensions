// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Resilience.Resilience;
using Moq;
using Polly;
using Polly.Extensions.Telemetry;
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
    private MeterListener _listener;
    private IServiceCollection _services;
    private Dictionary<string, object?>? _reportedTags;

    public ResilienceServiceCollectionExtensionsTests()
    {
        _listener = MeteringUtil.ListenPollyMetrics();

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

        _services.PostConfigure<TelemetryOptions>(options =>
        {
            options.Enrichers.Add(context =>
            {
                _reportedTags = context.Tags.ToDictionary(t => t.Key, t => t.Value);
            });
        });
    }

    public void Dispose() => _listener.Dispose();

    [Fact]
    public void AddResilienceEnrichment_NoOutcome_EnsureDimensions()
    {
        Build();
        _telemetry!.Report("dummy-event", ResilienceContext.Get(), string.Empty);

        _reportedTags!["failure-reason"].Should().BeNull();
        _reportedTags!["failure-source"].Should().BeNull();
        _reportedTags!["failure-summary"].Should().BeNull();
        _reportedTags!["dep-name"].Should().BeNull();
        _reportedTags!["req-name"].Should().BeNull();
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

        _reportedTags!["failure-reason"].Should().Be("InvalidOperationException");
        _reportedTags!["failure-summary"].Should().Be("type:desc:details");
        _reportedTags!["failure-source"].Should().Be("my-source");
    }

    [Fact]
    public void AddResilienceEnrichment_Outcome_EnsureDimensions()
    {
        _services.ConfigureFailureResultContext<string>(v => FailureResultContext.Create("my-source", "my-reason", v));

        Build();
        _telemetry!.Report(
            "dummy-event",
            new OutcomeArguments<string, string>(ResilienceContext.Get(), Outcome.FromResult("string-result"), string.Empty));

        _reportedTags!["failure-source"].Should().Be("my-source");
        _reportedTags!["failure-reason"].Should().Be("my-reason");
        _reportedTags!["failure-summary"].Should().Be("string-result");
    }

    [Fact]
    public void AddResilienceEnrichment_RequestMetadata_EnsureDimensions()
    {
        var context = ResilienceContext.Get();
        context.SetRequestMetadata(new RequestMetadata { RequestName = "my-req", DependencyName = "my-dep" });

        Build();
        _telemetry!.Report("dummy-event", context, string.Empty);

        _reportedTags!["dep-name"].Should().Be("my-dep");
        _reportedTags!["req-name"].Should().Be("my-req");
    }

    private void Build()
    {
        _services.BuildServiceProvider().GetRequiredService<ResilienceStrategyProvider<string>>().GetStrategy("dummy");
    }
}

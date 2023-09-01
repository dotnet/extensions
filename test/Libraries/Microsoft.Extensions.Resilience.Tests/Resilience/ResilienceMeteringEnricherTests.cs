// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Resilience.Internal;
using Moq;
using Polly;
using Polly.Telemetry;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test.Resilience;

public class ResilienceMeteringEnricherTests
{
    private readonly Mock<IExceptionSummarizer> _summarizer = new(MockBehavior.Strict);
    private readonly List<IOutgoingRequestContext> _outgoingRequestContexts = new();
    private readonly FailureEventMetricsOptions _options = new();

    private List<KeyValuePair<string, object?>> _tags = new();

    private IReadOnlyDictionary<string, object?> Tags => _tags.ToDictionary(v => v.Key, v => v.Value);

    [Fact]
    public void AddResilienceEnrichment_NoOutcome_EnsureDimensions()
    {
        CreateSut().Enrich(CreateEnrichmentContext<string>(null));

        _tags.Should().BeEmpty();
    }

    [Fact]
    public void AddResilienceEnrichment_Exception_EnsureDimensions()
    {
        _summarizer.Setup(v => v.Summarize(It.IsAny<Exception>())).Returns(new ExceptionSummary("type", "desc", "details"));

        CreateSut().Enrich(CreateEnrichmentContext<string>(Outcome.FromException<string>(new InvalidOperationException { Source = "my-source" })));

        Tags["failure-reason"].Should().Be("InvalidOperationException");
        Tags["failure-summary"].Should().Be("type:desc:details");
        Tags["failure-source"].Should().Be("my-source");
    }

    [Fact]
    public void AddResilienceEnrichment_Outcome_EnsureDimensions()
    {
        _options.ConfigureFailureResultContext<string>(v => FailureResultContext.Create("my-source", "my-reason", v));

        CreateSut().Enrich(CreateEnrichmentContext<string>(Outcome.FromResult("string-result")));

        Tags["failure-source"].Should().Be("my-source");
        Tags["failure-reason"].Should().Be("my-reason");
        Tags["failure-summary"].Should().Be("string-result");
    }

    [Fact]
    public void AddResilienceEnrichment_RequestMetadata_EnsureDimensions()
    {
        CreateSut().Enrich(CreateEnrichmentContext<string>(
            Outcome.FromResult("string-result"),
            context => context.SetRequestMetadata(new RequestMetadata { RequestName = "my-req", DependencyName = "my-dep" })));

        Tags["dep-name"].Should().Be("my-dep");
        Tags["req-name"].Should().Be("my-req");
    }

    [Fact]
    public void AddResilienceEnrichment_RequestMetadataFromOutgoingRequestContext_EnsureDimensions()
    {
        var requestMetadata = new RequestMetadata { RequestName = "my-req", DependencyName = "my-dep" };
        _outgoingRequestContexts.Add(Mock.Of<IOutgoingRequestContext>(v => v.RequestMetadata == requestMetadata));

        CreateSut().Enrich(CreateEnrichmentContext<string>());

        Tags["dep-name"].Should().Be("my-dep");
        Tags["req-name"].Should().Be("my-req");
    }

    private EnrichmentContext<T, object> CreateEnrichmentContext<T>(Outcome<T>? outcome = null, Action<ResilienceContext>? configure = null)
    {
        var source = new ResilienceTelemetrySource("A", "B", "C");
        var context = ResilienceContextPool.Shared.Get();
        configure?.Invoke(context);

        return new EnrichmentContext<T, object>(
            new TelemetryEventArguments<T, object>(
                source,
                new ResilienceEvent(ResilienceEventSeverity.Warning, "dummy"),
                context,
                new object(),
                outcome),
            _tags);
    }

    private ResilienceMeteringEnricher CreateSut() => new(Options.Options.Create(_options), _outgoingRequestContexts, _summarizer.Object);
}

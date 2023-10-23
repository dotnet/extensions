// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Resilience.Internal;
using Moq;
using Polly;
using Polly.Telemetry;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test.Resilience;

public class ResilienceMetricsEnricherTests
{
    private IOutgoingRequestContext? _outgoingRequestContext;
    private Mock<IExceptionSummarizer>? _summarizer = new(MockBehavior.Strict);

    private List<KeyValuePair<string, object?>> _tags = [];

    private IReadOnlyDictionary<string, object?> Tags => _tags.ToDictionary(v => v.Key, v => v.Value);

    [Fact]
    public void AddResilienceEnricher_NoOutcome_EnsureDimensions()
    {
        CreateSut().Enrich(CreateEnrichmentContext<string>(null));

        _tags.Should().BeEmpty();
    }

    [Fact]
    public void AddResilienceEnricher_Exception_EnsureDimensions()
    {
        _summarizer!.Setup(v => v.Summarize(It.IsAny<Exception>())).Returns(new ExceptionSummary("type", "desc", "details"));

        CreateSut().Enrich(CreateEnrichmentContext<string>(Outcome.FromException<string>(new InvalidOperationException { Source = "my-source" })));

        Tags["error.type"].Should().Be("type:desc:details");
    }

    [Fact]
    public void AddResilienceEnricher_ExceptionWithNoSummarizer_EnsureNoDimensions()
    {
        _summarizer = null;

        CreateSut().Enrich(CreateEnrichmentContext<string>(Outcome.FromException<string>(new InvalidOperationException { Source = "my-source" })));

        Tags.Should().NotContainKey("error.type");
    }

    [Fact]
    public void AddResilienceEnricher_RequestMetadata_EnsureDimensions()
    {
        CreateSut().Enrich(CreateEnrichmentContext<string>(
            Outcome.FromResult("string-result"),
            context => context.SetRequestMetadata(new RequestMetadata { RequestName = "my-req", DependencyName = "my-dep" })));

        Tags["request.dependency.name"].Should().Be("my-dep");
        Tags["request.name"].Should().Be("my-req");
    }

    [Fact]
    public void AddResilienceEnricher_RequestMetadataFromOutgoingRequestContext_EnsureDimensions()
    {
        var requestMetadata = new RequestMetadata { RequestName = "my-req", DependencyName = "my-dep" };
        _outgoingRequestContext = Mock.Of<IOutgoingRequestContext>(v => v.RequestMetadata == requestMetadata);

        CreateSut().Enrich(CreateEnrichmentContext<string>());

        Tags["request.dependency.name"].Should().Be("my-dep");
        Tags["request.name"].Should().Be("my-req");
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

    private ResilienceMetricsEnricher CreateSut() => new(_outgoingRequestContext, _summarizer?.Object);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Resilience.Internal;
using Moq;
using Polly;
using Polly.Telemetry;
using Xunit;

namespace Microsoft.Extensions.Resilience.Test.Resilience;

public class ResilienceMetricsEnricherTests
{
    private readonly List<IOutgoingRequestContext> _outgoingRequestContexts = [];
    private readonly FailureEventMetricsOptions _options = new();
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

        Tags.Should().ContainKey("dotnet.resilience.failure.reason").WhoseValue.Should().Be("InvalidOperationException");
        Tags.Should().ContainKey("dotnet.resilience.failure.summary").WhoseValue.Should().Be("type:desc:details");
        Tags.Should().ContainKey("dotnet.resilience.failure.source").WhoseValue.Should().Be("my-source");
    }

    [Fact]
    public void AddResilienceEnricher_ExceptionWithNoSummarizer_EnsureNoDimensions()
    {
        _summarizer = null;

        CreateSut().Enrich(CreateEnrichmentContext<string>(Outcome.FromException<string>(new InvalidOperationException { Source = "my-source" })));

        Tags.Should().NotContainKey("dotnet.resilience.failure.reason");
        Tags.Should().NotContainKey("dotnet.resilience.failure.summary");
        Tags.Should().NotContainKey("dotnet.resilience.failure.source");
    }

    [Fact]
    public void AddResilienceEnricher_Outcome_EnsureDimensions()
    {
        _options.ConfigureFailureResultContext<string>(v => FailureResultContext.Create("my-source", "my-reason", v));

        CreateSut().Enrich(CreateEnrichmentContext<string>(Outcome.FromResult("string-result")));

        Tags.Should().ContainKey("dotnet.resilience.failure.source").WhoseValue.Should().Be("my-source");
        Tags.Should().ContainKey("dotnet.resilience.failure.reason").WhoseValue.Should().Be("my-reason");
        Tags.Should().ContainKey("dotnet.resilience.failure.summary").WhoseValue.Should().Be("string-result");
    }

    [Fact]
    public void AddResilienceEnricher_RequestMetadata_EnsureDimensions()
    {
        CreateSut().Enrich(CreateEnrichmentContext<string>(
            Outcome.FromResult("string-result"),
            context => context.SetRequestMetadata(new RequestMetadata { RequestName = "my-req", DependencyName = "my-dep" })));

        Tags.Should().ContainKey("dotnet.resilience.dependency.name").WhoseValue.Should().Be("my-dep");
        Tags.Should().ContainKey("dotnet.resilience.request.name").WhoseValue.Should().Be("my-req");
    }

    [Fact]
    public void AddResilienceEnricher_RequestMetadataFromOutgoingRequestContext_EnsureDimensions()
    {
        var requestMetadata = new RequestMetadata { RequestName = "my-req", DependencyName = "my-dep" };
        _outgoingRequestContexts.Add(Mock.Of<IOutgoingRequestContext>(v => v.RequestMetadata == requestMetadata));

        CreateSut().Enrich(CreateEnrichmentContext<string>());

        Tags.Should().ContainKey("dotnet.resilience.dependency.name").WhoseValue.Should().Be("my-dep");
        Tags.Should().ContainKey("dotnet.resilience.request.name").WhoseValue.Should().Be("my-req");
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

    private ResilienceMetricsEnricher CreateSut() => new(Options.Options.Create(_options), _outgoingRequestContexts, _summarizer?.Object);
}

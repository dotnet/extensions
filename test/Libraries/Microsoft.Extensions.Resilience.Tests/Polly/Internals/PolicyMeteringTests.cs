// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Extensions.Telemetry.Metering;
using Microsoft.Extensions.Telemetry.Testing.Metering;
using Moq;
using Polly;
using Xunit;

#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
#pragma warning disable CA1063 // Implement IDisposable Correctly

namespace Microsoft.Extensions.Resilience.Polly.Test.Internals;

public class PolicyMeteringTests : IDisposable
{
    private const string PipelineName = "pipeline-name";

    private const string PipelineKey = "pipeline-key";

    private const string ResultType = "String";

    private const string MetricName = @"R9\Resilience\Policies";

    private readonly Mock<IExceptionSummarizer> _summarizer;
    private readonly Mock<IOutgoingRequestContext> _outgoingContext;
    private readonly Meter<PolicyMetering> _meter;
    private readonly MetricCollector _metricCollector;
    private PolicyMetering _metering;
    private FailureResultContext _context;

    public PolicyMeteringTests()
    {
        _summarizer = new Mock<IExceptionSummarizer>(MockBehavior.Strict);
        _outgoingContext = new Mock<IOutgoingRequestContext>(MockBehavior.Strict);

        _meter = new();
        _metricCollector = new(_meter);

        var services = new ServiceCollection();
        services.TryAddSingleton(_outgoingContext.Object);
        services.ConfigureFailureResultContext<string>(v => _context);

        _metering = new PolicyMetering(_meter, _summarizer.Object, services.BuildServiceProvider());
    }

    public void Dispose()
    {
        _metricCollector.Dispose();
        _meter.Dispose();
    }

    [Fact]
    public void Initialize_Twice_Throws()
    {
        Initialize();
        Assert.Throws<InvalidOperationException>(() => Initialize());
    }

    [InlineData("", TelemetryConstants.Unknown)]
    [InlineData(null, TelemetryConstants.Unknown)]
    [InlineData(TelemetryConstants.Unknown, TelemetryConstants.Unknown)]
    [InlineData("test", "test")]
    [Theory]
    public void Initialize_EnsurePipelineKeyRespected(string pipelineKey, string expectedKey)
    {
        Initialize(pipelineKey);

        RecordEvent(true, "policy", "ev", null);

        Assert.Equal(expectedKey, Counter.LatestWritten!.GetDimension(ResilienceDimensions.PipelineKey));
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void NotInitialized_DoesNotMeter(bool typed)
    {
        RecordEvent(typed, "policy", "ev", null);

        Counter.AllValues.Should().BeEmpty();
    }

    [Fact]
    public void NoOutgoingContext_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        services.AddOptions();

        _metering = new PolicyMetering(_meter, _summarizer.Object, services.BuildServiceProvider());

        Initialize();

        RecordEvent(true, "policy", "ev", null);

        Assert.Equal(TelemetryConstants.Unknown, Counter.LatestWritten!.GetDimension(ResilienceDimensions.DependencyName));
        Assert.Equal(TelemetryConstants.Unknown, Counter.LatestWritten!.GetDimension(ResilienceDimensions.RequestName));
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void RecordEvent_NullException_Ok(bool typed)
    {
        Initialize();

        RecordEvent(typed, "policy", "ev", null);

        var latest = Counter.LatestWritten;

        Assert.Equal(PipelineName, latest!.GetDimension(ResilienceDimensions.PipelineName));
        Assert.Equal(PipelineKey, latest.GetDimension(ResilienceDimensions.PipelineKey));
        Assert.Equal(ResultType, latest.GetDimension(ResilienceDimensions.ResultType));
        Assert.Equal("policy", latest.GetDimension(ResilienceDimensions.PolicyName));
        Assert.Equal("ev", latest.GetDimension(ResilienceDimensions.EventName));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(ResilienceDimensions.FailureSource));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(ResilienceDimensions.FailureReason));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(ResilienceDimensions.FailureSummary));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(ResilienceDimensions.DependencyName));
        Assert.Equal(TelemetryConstants.Unknown, latest.GetDimension(ResilienceDimensions.RequestName));
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void RecordEvent_Exception_Ok(bool typed)
    {
        Initialize();
        var er = new InvalidOperationException();

        _summarizer.Setup(v => v.Summarize(er)).Returns(new ExceptionSummary("type", "desc", "details"));

        RecordEvent(typed, "policy", "ev", er);

        var latest = Counter.LatestWritten;

        Assert.Equal(TelemetryConstants.Unknown, latest!.GetDimension(ResilienceDimensions.FailureSource));
        Assert.Equal("InvalidOperationException", latest.GetDimension(ResilienceDimensions.FailureReason));
        Assert.Equal("type:desc:details", latest.GetDimension(ResilienceDimensions.FailureSummary));
    }

    [Fact]
    public void RecordEvent_FailureResult_Ok()
    {
        Initialize();
        _context = FailureResultContext.Create("src", "reason", "summary");
        _metering.RecordEvent("policy", "ev", new DelegateResult<string>("test"), new Context());

        var latest = Counter.LatestWritten;

        Assert.Equal("src", latest!.GetDimension(ResilienceDimensions.FailureSource));
        Assert.Equal("reason", latest.GetDimension(ResilienceDimensions.FailureReason));
        Assert.Equal("summary", latest.GetDimension(ResilienceDimensions.FailureSummary));
    }

    [InlineData(true)]
    [InlineData(false)]
    [Theory]
    public void RecordEvent_RequestMetadata(bool typed)
    {
        Initialize();
        var er = new InvalidOperationException();
        var metadata1 = new RequestMetadata { DependencyName = "dep", RequestName = "req" };
        var metadata2 = new RequestMetadata { DependencyName = "dep2", RequestName = "req2" };

        _outgoingContext.Setup(o => o.RequestMetadata).Returns(metadata1);
        RecordEvent(typed, "policy", "ev", null, new Context());

        var latest = Counter.LatestWritten;

        Assert.Equal("dep", latest!.GetDimension(ResilienceDimensions.DependencyName));
        Assert.Equal("req", latest.GetDimension(ResilienceDimensions.RequestName));

        var ctx = new Context
        {
            [TelemetryConstants.RequestMetadataKey] = metadata2
        };
        RecordEvent(typed, "policy", "ev", null, ctx);

        latest = Counter.LatestWritten;
        Assert.Equal("dep2", latest!.GetDimension(ResilienceDimensions.DependencyName));
        Assert.Equal("req2", latest.GetDimension(ResilienceDimensions.RequestName));
    }

    private void RecordEvent(bool typed, string policyName, string eventName, Exception? exception, Context? context = null)
    {
        if (typed)
        {
            _metering.RecordEvent(policyName, eventName, exception != null ? new DelegateResult<string>(exception) : null, context ?? new Context());
        }
        else
        {
            _metering.RecordEvent(policyName, eventName, exception, context ?? new Context());
        }
    }

    private MetricValuesHolder<long> Counter => _metricCollector.GetCounterValues<long>(MetricName)!;

    private void Initialize(string pipelineKey = PipelineKey)
    {
        _metering.Initialize(PipelineId.Create<string>(PipelineName, pipelineKey));
        _outgoingContext.Setup(o => o.RequestMetadata).Returns<RequestMetadata>(null);
    }
}

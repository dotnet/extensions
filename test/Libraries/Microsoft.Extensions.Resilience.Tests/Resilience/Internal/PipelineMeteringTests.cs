// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
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

public class PipelineMeteringTests : IDisposable
{
    private const string PipelineName = "pipeline-name";

    private const string PipelineKey = "pipeline-key";

    private const string ResultType = "String";

    private const string MetricName = @"R9\Resilience\Pipelines";

    private readonly Mock<IExceptionSummarizer> _summarizer;
    private readonly Mock<IOutgoingRequestContext> _outgoingContext;
    private readonly Meter<PipelineMetering> _meter;
    private PipelineMetering _metering;

    public PipelineMeteringTests()
    {
        _summarizer = new Mock<IExceptionSummarizer>(MockBehavior.Strict);
        _outgoingContext = new Mock<IOutgoingRequestContext>(MockBehavior.Strict);
        _meter = new();
        Counter = new(_meter, MetricName);
        _metering = new PipelineMetering(_meter, _summarizer.Object, new[] { _outgoingContext.Object });
    }

    public void Dispose()
    {
        Counter.Dispose();
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

        RecordPipelineExecution(null);

        Assert.Equal(expectedKey, Counter.LastMeasurement?.Tags[ResilienceDimensions.PipelineKey]);
    }

    [Fact]
    public void NotInitialized_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => RecordPipelineExecution(null));
    }

    [Fact]
    public void NoOutgoingContext_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        services.AddOptions();

        _metering = new PipelineMetering(_meter, _summarizer.Object, Array.Empty<IOutgoingRequestContext>());

        Initialize();

        RecordPipelineExecution(null);

        Assert.Equal(TelemetryConstants.Unknown, Counter.LastMeasurement?.Tags[ResilienceDimensions.DependencyName]);
        Assert.Equal(TelemetryConstants.Unknown, Counter.LastMeasurement?.Tags[ResilienceDimensions.RequestName]);
    }

    [Fact]
    public void RecordEvent_NullException_Ok()
    {
        Initialize();

        RecordPipelineExecution(null);

        var latest = Counter.LastMeasurement!;

        Assert.Equal(PipelineName, latest.Tags[ResilienceDimensions.PipelineName]);
        Assert.Equal(PipelineKey, latest.Tags[ResilienceDimensions.PipelineKey]);
        Assert.Equal(ResultType, latest.Tags[ResilienceDimensions.ResultType]);
        Assert.Equal(TelemetryConstants.Unknown, latest.Tags[ResilienceDimensions.FailureSource]);
        Assert.Equal(TelemetryConstants.Unknown, latest.Tags[ResilienceDimensions.FailureReason]);
        Assert.Equal(TelemetryConstants.Unknown, latest.Tags[ResilienceDimensions.FailureSummary]);
        Assert.Equal(TelemetryConstants.Unknown, latest.Tags[ResilienceDimensions.DependencyName]);
        Assert.Equal(TelemetryConstants.Unknown, latest.Tags[ResilienceDimensions.RequestName]);
    }

    [Fact]
    public void RecordEvent_Exception_Ok()
    {
        Initialize();
        var er = new InvalidOperationException();

        _summarizer.Setup(v => v.Summarize(er)).Returns(new ExceptionSummary("type", "desc", "details"));

        RecordPipelineExecution(er);

        var latest = Counter.LastMeasurement!;

        Assert.Equal(TelemetryConstants.Unknown, latest.Tags[ResilienceDimensions.FailureSource]);
        Assert.Equal("InvalidOperationException", latest.Tags[ResilienceDimensions.FailureReason]);
        Assert.Equal("type:desc:details", latest.Tags[ResilienceDimensions.FailureSummary]);
    }

    [Fact]
    public void RecordEvent_RequestMetadata()
    {
        Initialize();
        var er = new InvalidOperationException();
        var metadata1 = new RequestMetadata { DependencyName = "dep", RequestName = "req" };
        var metadata2 = new RequestMetadata { DependencyName = "dep2", RequestName = "req2" };

        _outgoingContext.Setup(o => o.RequestMetadata).Returns(metadata1);
        RecordPipelineExecution(null, new Context());

        var latest = Counter.LastMeasurement!;

        Assert.Equal("dep", latest.Tags[ResilienceDimensions.DependencyName]);
        Assert.Equal("req", latest.Tags[ResilienceDimensions.RequestName]);

        var ctx = new Context
        {
            [TelemetryConstants.RequestMetadataKey] = metadata2
        };
        RecordPipelineExecution(null, ctx);

        latest = Counter.LastMeasurement!;

        Assert.Equal("dep2", latest.Tags[ResilienceDimensions.DependencyName]);
        Assert.Equal("req2", latest.Tags[ResilienceDimensions.RequestName]);
    }

    private void RecordPipelineExecution(Exception? exception, Context? context = null)
    {
        _metering.RecordPipelineExecution(1, exception, context ?? new Context());

    }

    private MetricCollector<long> Counter { get; }

    private void Initialize(string pipelineKey = PipelineKey)
    {
        _metering.Initialize(PipelineId.Create<string>(PipelineName, pipelineKey));
        _outgoingContext.Setup(o => o.RequestMetadata).Returns<RequestMetadata>(null);
    }
}

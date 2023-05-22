// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;

public class PipelineTelemetryTests
{
    [Fact]
    public async Task Create_EnsureMetering()
    {
        var metering = new Mock<IPipelineMetering>(MockBehavior.Strict);
        var policy = PipelineTelemetry.Create(PipelineId.Create("a", "b"), Policy.NoOpAsync<string>(), metering.Object, NullLogger<PipelineTelemetry>.Instance, TimeProvider.System);
        var nonGenericPolicy = PipelineTelemetry.Create(PipelineId.Create("a", "b"), Policy.NoOpAsync(), metering.Object, NullLogger<PipelineTelemetry>.Instance, TimeProvider.System);

        metering.Setup(o => o.RecordPipelineExecution(It.IsAny<long>(), null, It.IsAny<Context>()));
        await policy.ExecuteAsync(() => Task.FromResult("dummy"));
        await nonGenericPolicy.ExecuteAsync(() => Task.FromResult("dummy"));

        metering.Verify(o => o.RecordPipelineExecution(It.IsAny<long>(), null, It.IsAny<Context>()), Times.Exactly(2));

        var error = new InvalidOperationException();
        metering.Setup(o => o.RecordPipelineExecution(It.IsAny<long>(), error, It.IsAny<Context>()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => policy.ExecuteAsync(() => throw error));
        await Assert.ThrowsAsync<InvalidOperationException>(() => nonGenericPolicy.ExecuteAsync(() => throw error));

        metering.Verify(o => o.RecordPipelineExecution(It.IsAny<long>(), error, It.IsAny<Context>()), Times.Exactly(2));
    }

    [InlineData("Dummy", false)]
    [InlineData("", false)]
    [InlineData("Dummy", true)]
    [InlineData("", true)]
    [Theory]
    public async Task Create_EnsureLogging(string key, bool error)
    {
        var collector = new FakeLogCollector();
        var timeProvider = new FakeTimeProvider();
        var logger = new FakeLogger<PipelineTelemetry>(collector);
        var policy = PipelineTelemetry.Create(
            PipelineId.Create("a", key),
            Policy.NoOpAsync<string>(),
            Mock.Of<IPipelineMetering>(),
            logger,
            timeProvider);

        var nonGenericPolicy = PipelineTelemetry.Create(
            PipelineId.Create("b", key),
            Policy.NoOpAsync(),
            Mock.Of<IPipelineMetering>(),
            logger,
            timeProvider);

        timeProvider.Advance(TimeSpan.FromMinutes(1));

        if (error)
        {
            try
            {
                await policy.ExecuteAsync(() => throw new InvalidOperationException());
            }
            catch (InvalidOperationException)
            {
                // ok
            }

            try
            {
                await nonGenericPolicy.ExecuteAsync(() => throw new InvalidOperationException());
            }
            catch (InvalidOperationException)
            {
                // ok
            }
        }
        else
        {
            await policy.ExecuteAsync(() => Task.FromResult("dummy"));
            await nonGenericPolicy.ExecuteAsync(() => Task.FromResult("dummy"));
        }

        if (string.IsNullOrEmpty(key))
        {
            key = TelemetryConstants.Unknown;
        }

        var entries = collector.GetSnapshot();
        Assert.Equal(4, entries.Count);
        Assert.Equal($"Executing pipeline. Pipeline Name: a, Pipeline Key: {key}", entries[0].Message);
        Assert.Equal($"Executing pipeline. Pipeline Name: b, Pipeline Key: {key}", entries[2].Message);

        if (error)
        {
            Assert.Equal($"Pipeline execution failed in 0ms. Pipeline Name: a, Pipeline Key: {key}", entries[1].Message);
            Assert.Equal($"Pipeline execution failed in 0ms. Pipeline Name: b, Pipeline Key: {key}", entries[3].Message);
        }
        else
        {
            Assert.Equal($"Pipeline executed in 0ms. Pipeline Name: a, Pipeline Key: {key}", entries[1].Message);
            Assert.Equal($"Pipeline executed in 0ms. Pipeline Name: b, Pipeline Key: {key}", entries[3].Message);
        }
    }
}

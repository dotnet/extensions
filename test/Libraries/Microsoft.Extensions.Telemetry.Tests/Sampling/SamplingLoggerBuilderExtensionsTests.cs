// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Diagnostics.Sampling;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Sampling;
public class SamplingLoggerBuilderExtensionsTests
{
    [Fact]
    public void WhenArgumentIsNull_Throws()
    {
        var builder = null as ILoggingBuilder;

        var action = () => SamplingLoggerBuilderExtensions.AddTraceBasedSampling(builder!);
        Assert.Throws<ArgumentNullException>(action);

        var action2 = () => SamplingLoggerBuilderExtensions.AddRatioBasedSampler(builder!, 1.0);
        Assert.Throws<ArgumentNullException>(action);

        var action3 = () => SamplingLoggerBuilderExtensions.AddSampler(builder!, (_) => true);
        Assert.Throws<ArgumentNullException>(action);

        var action4 = () => SamplingLoggerBuilderExtensions.AddSampler<MySampler>(builder!);
        Assert.Throws<ArgumentNullException>(action);

        var action5 = () => SamplingLoggerBuilderExtensions.AddSampler(builder!, new MySampler());
        Assert.Throws<ArgumentNullException>(action);
    }

    private class MySampler : LoggerSampler
    {
        public override bool ShouldSample(SamplingParameters _) => true;
    }
}

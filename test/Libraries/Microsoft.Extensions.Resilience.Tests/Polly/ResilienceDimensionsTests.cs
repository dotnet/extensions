// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test;

public class ResilienceDimensionsTests
{
    [Fact]
    public void DimensionNames_Ok()
    {
        var names = ResilienceDimensions.DimensionNames;

        Assert.Equal(10, names.Count);

        Assert.Equal(ResilienceDimensions.PipelineName, names[0]);
        Assert.Equal(ResilienceDimensions.PipelineKey, names[1]);
        Assert.Equal(ResilienceDimensions.ResultType, names[2]);
        Assert.Equal(ResilienceDimensions.PolicyName, names[3]);
        Assert.Equal(ResilienceDimensions.EventName, names[4]);
        Assert.Equal(ResilienceDimensions.FailureSource, names[5]);
        Assert.Equal(ResilienceDimensions.FailureReason, names[6]);
        Assert.Equal(ResilienceDimensions.FailureSummary, names[7]);
        Assert.Equal(ResilienceDimensions.DependencyName, names[8]);
        Assert.Equal(ResilienceDimensions.RequestName, names[9]);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test.Internals;

public class PipelineIdTests
{
    [InlineData("pipeline", "key", "String-pipeline-key")]
    [InlineData("pipeline", null, "String-pipeline")]
    [InlineData("pipeline", "", "String-pipeline")]
    [Theory]
    public void PolicyPipelineKey_Typed_Ok(string pipeline, string key, string expectedResult)
    {
        Assert.Equal(expectedResult, PipelineId.Create<string>(pipeline, key).PolicyPipelineKey);
    }

    [InlineData("pipeline", "key", "pipeline-key")]
    [InlineData("pipeline", null, "pipeline")]
    [InlineData("pipeline", "", "pipeline")]
    [Theory]
    public void PolicyPipelineKey_NonTyped_Ok(string pipeline, string key, string expectedResult)
    {
        Assert.Equal(expectedResult, PipelineId.Create(pipeline, key).PolicyPipelineKey);
    }

    [Fact]
    public void Create_Ok()
    {
        Assert.Throws<ArgumentNullException>(() => PipelineId.Create<string>(null!, "key"));
        Assert.Throws<ArgumentException>(() => PipelineId.Create<string>(string.Empty, "key"));
        Assert.Throws<ArgumentNullException>(() => PipelineId.Create(null!, "key"));
        Assert.Throws<ArgumentException>(() => PipelineId.Create(string.Empty, "key"));

        var id = PipelineId.Create("dummy", "key");

        Assert.Equal("dummy", id.PipelineName);
        Assert.Equal("key", id.PipelineKey);

        id = PipelineId.Create<string>("dummy", "key");

        Assert.Equal("dummy", id.PipelineName);
        Assert.Equal("key", id.PipelineKey);
        Assert.Equal("String", id.ResultType);
    }
}

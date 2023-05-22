// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Http.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internals;
public class PipelineNameHelperTest
{
    [Fact]
    public void GetPipelineName_Ok()
    {
        Assert.Equal("client-pipeline", PipelineNameHelper.GetPipelineName("client", "pipeline"));
    }
}

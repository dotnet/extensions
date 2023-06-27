// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Http.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Resilience;
public class StrategyNameHelperTest
{
    [Fact]
    public void GetPipelineName_Ok()
    {
        Assert.Equal("client-pipeline", StrategyNameHelper.GetName("client", "pipeline"));
    }
}

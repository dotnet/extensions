// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Resilience.Internal;
using Xunit;

namespace Microsoft.Extensions.Resilience.Internal.Test;
public class OptionsNameHelperTest
{
    [Fact]
    public void GetPolicyOptionsName_Ok()
    {
        var name = OptionsNameHelper.GetPolicyOptionsName(SupportedPolicies.RetryPolicy, "pipeline", "retry");

        Assert.Equal("pipeline-RetryPolicy-retry", name);
    }
}

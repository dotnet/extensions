// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AsyncState.Test;
public class FeaturesPooledPolicyTests
{
    [Fact]
    public void Return_ShouldBeTrue()
    {
        var policy = new FeaturesPooledPolicy();

        Assert.True(policy.Return(new Features()));
    }

    [Fact]
    public void Return_ShouldNullList()
    {
        var policy = new FeaturesPooledPolicy();

        var features = policy.Create();
        features.Set(0, string.Empty);
        features.Set(1, Array.Empty<int>());
        features.Set(2, new object());

        Assert.True(policy.Return(features));
        Assert.Null(features.Get(0));
        Assert.Null(features.Get(1));
        Assert.Null(features.Get(2));
    }
}

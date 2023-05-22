// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.AsyncState.Test;
public class FeaturesPooledPolicyTests
{
    [Fact]
    public void Return_ShouldBeTrue()
    {
        var policy = new FeaturesPooledPolicy();

        Assert.True(policy.Return(new List<object?>()));
    }

    [Fact]
    public void Return_ShouldNullList()
    {
        var policy = new FeaturesPooledPolicy();

        var list = policy.Create();
        list.Add(string.Empty);
        list.Add(Array.Empty<int>());
        list.Add(new object());

        Assert.True(policy.Return(list));
        Assert.All(list, el => Assert.Null(el));
    }
}

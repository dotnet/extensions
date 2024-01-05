// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.AsyncState.Test;
public class ConcurrentDictionaryPooledPolicyTests
{
    [Fact]
    public void Return_ShouldBeTrue()
    {
        var policy = new ConcurrentDictionaryPooledPolicy();

        Assert.True(policy.Return([]));
    }

    [Fact]
    public void Return_ShouldNullList()
    {
        var policy = new ConcurrentDictionaryPooledPolicy();

        var dictionary = policy.Create();
        dictionary[new AsyncStateToken(0)] = string.Empty;
        dictionary[new AsyncStateToken(1)] = Array.Empty<int>();
        dictionary[new AsyncStateToken(2)] = new object();

        Assert.True(policy.Return(dictionary));
        Assert.True(dictionary.IsEmpty);
    }
}

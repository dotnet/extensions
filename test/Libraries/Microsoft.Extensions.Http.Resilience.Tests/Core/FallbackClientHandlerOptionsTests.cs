// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test;

public class FallbackClientHandlerOptionsTests
{
    private readonly FallbackClientHandlerOptions _testObject;

    public FallbackClientHandlerOptionsTests()
    {
        _testObject = new FallbackClientHandlerOptions();
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var instance = new FallbackClientHandlerOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void FallbackPolicyOptions_ShouldGetAndSet()
    {
        var testValue = new HttpFallbackPolicyOptions
        {
            ShouldHandleResultAsError = response => !response.IsSuccessStatusCode
        };

        _testObject.FallbackPolicyOptions = testValue;
        Assert.Equal(testValue, _testObject.FallbackPolicyOptions);
    }

    [Fact]
    public void BaseFallbackUri_ShouldGetAndSet()
    {
        var testValue = new Uri("https://lalalala.com");

        _testObject.BaseFallbackUri = testValue;
        Assert.Equal(testValue, _testObject.BaseFallbackUri);
    }
}

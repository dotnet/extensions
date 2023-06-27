// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Polly;

public class HttpRateLimiterStrategyOptionsTests
{
#pragma warning disable S2330
    private readonly HttpRateLimiterStrategyOptions _testObject;

    public HttpRateLimiterStrategyOptionsTests()
    {
        _testObject = new HttpRateLimiterStrategyOptions();
    }

    [Fact]
    public void Ctor_Defaults()
    {
        _testObject.DefaultRateLimiterOptions.Should().NotBeNull();
        _testObject.RateLimiter.Should().BeNull();
        _testObject.OnRejected.Should().BeNull();
        _testObject.DefaultRateLimiterOptions.QueueLimit.Should().Be(0);
        _testObject.DefaultRateLimiterOptions.PermitLimit.Should().Be(1000);
        _testObject.DefaultRateLimiterOptions.QueueProcessingOrder.Should().Be(QueueProcessingOrder.OldestFirst);
    }
}

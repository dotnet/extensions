// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Resilience;

public class HttpStandardHedgingResilienceOptionsTests
{
    private readonly HttpStandardHedgingResilienceOptions _options;

    public HttpStandardHedgingResilienceOptionsTests()
    {
        _options = new HttpStandardHedgingResilienceOptions();
    }

    [Fact]
    public void Ctor_EnsureDefaults()
    {
        _options.TotalRequestTimeoutOptions.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        _options.EndpointOptions.TimeoutOptions.Timeout.Should().Be(TimeSpan.FromSeconds(10));

        _options.TotalRequestTimeoutOptions.StrategyName.Should().Be("StandardHedging-TotalRequestTimeout");
        _options.HedgingOptions.StrategyName.Should().Be("StandardHedging-Hedging");
        _options.EndpointOptions.CircuitBreakerOptions.StrategyName.Should().Be("StandardHedging-CircuitBreaker");
        _options.EndpointOptions.TimeoutOptions.StrategyName.Should().Be("StandardHedging-AttemptTimeout");
        _options.EndpointOptions.RateLimiterOptions.StrategyName.Should().Be("StandardHedging-RateLimiter");
    }
}

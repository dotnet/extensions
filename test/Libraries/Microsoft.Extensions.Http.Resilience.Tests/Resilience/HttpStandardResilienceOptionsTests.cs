﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using FluentAssertions;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Resilience;

public class HttpStandardResilienceOptionsTests
{
    private readonly HttpStandardResilienceOptions _options;

    public HttpStandardResilienceOptionsTests()
    {
        _options = new HttpStandardResilienceOptions();
    }

    [Fact]
    public void Ctor_EnsureDefaults()
    {
        _options.AttemptTimeoutOptions.Timeout.Should().Be(TimeSpan.FromSeconds(10));
        _options.TotalRequestTimeoutOptions.Timeout.Should().Be(TimeSpan.FromSeconds(30));

        _options.TotalRequestTimeoutOptions.StrategyName.Should().Be("Standard-TotalRequestTimeout");
        _options.RateLimiterOptions.StrategyName.Should().Be("Standard-RateLimiter");
        _options.RetryOptions.StrategyName.Should().Be("Standard-Retry");
        _options.CircuitBreakerOptions.StrategyName.Should().Be("Standard-CircuitBreaker");
        _options.AttemptTimeoutOptions.StrategyName.Should().Be("Standard-AttemptTimeout");
    }
}

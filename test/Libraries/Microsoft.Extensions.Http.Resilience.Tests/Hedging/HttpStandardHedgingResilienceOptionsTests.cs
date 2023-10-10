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
        _options.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(30));
        _options.Endpoint.Timeout.Timeout.Should().Be(TimeSpan.FromSeconds(10));

        _options.TotalRequestTimeout.Name.Should().Be("StandardHedging-TotalRequestTimeout");
        _options.Hedging.Name.Should().Be("StandardHedging-Hedging");
        _options.Endpoint.CircuitBreaker.Name.Should().Be("StandardHedging-CircuitBreaker");
        _options.Endpoint.Timeout.Name.Should().Be("StandardHedging-AttemptTimeout");
        _options.Endpoint.RateLimiter.Name.Should().Be("StandardHedging-RateLimiter");
    }
}

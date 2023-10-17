// Licensed to the .NET Foundation under one or more agreements.
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
        _options.AttemptTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(10));
        _options.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(30));

        _options.TotalRequestTimeout.Name.Should().Be("Standard-TotalRequestTimeout");
        _options.RateLimiter.Name.Should().Be("Standard-RateLimiter");
        _options.Retry.Name.Should().Be("Standard-Retry");
        _options.CircuitBreaker.Name.Should().Be("Standard-CircuitBreaker");
        _options.AttemptTimeout.Name.Should().Be("Standard-AttemptTimeout");
    }
}

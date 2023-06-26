// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Internal;

public class RetryAfterHelperTests
{
    [Fact]
    public void TryParse_Delta_Ok()
    {
        using var response = new HttpResponseMessage();
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10));

        var parsed = RetryAfterHelper.TryParse(response, TimeProvider.System, out var retryAfter);

        parsed.Should().BeTrue();
        retryAfter.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void TryParse_Null_Ok()
    {
        using var response = new HttpResponseMessage();

        var parsed = RetryAfterHelper.TryParse(response, TimeProvider.System, out var retryAfter);

        parsed.Should().BeFalse();
        retryAfter.Should().Be(TimeSpan.MinValue);
    }

    [Fact]
    public void TryParse_Date_Ok()
    {
        var timeProvider = new FakeTimeProvider();

        using var response = new HttpResponseMessage();
        response.Headers.RetryAfter = new RetryConditionHeaderValue(timeProvider.GetUtcNow() + TimeSpan.FromDays(1));

        var parsed = RetryAfterHelper.TryParse(response, timeProvider, out var retryAfter);

        parsed.Should().BeTrue();
        retryAfter.Should().Be(TimeSpan.FromDays(1));
    }
}

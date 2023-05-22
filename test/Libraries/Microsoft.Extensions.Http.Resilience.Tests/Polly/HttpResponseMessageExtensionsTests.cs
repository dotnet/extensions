// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Net.Http;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Polly;

public class HttpResponseMessageExtensionsTests
{
    private readonly TimeProvider _fakeClock = new FakeTimeProvider();

    [Fact]
    public void RetryAfter_WhenNoHeaderFound_ShouldReturnZero()
    {
        using var httpResponseMessage = new HttpResponseMessage();
        Assert.Equal(TimeSpan.Zero, RetryAfterHelper.ParseRetryAfterHeader(null!, _fakeClock));
        Assert.Equal(TimeSpan.Zero, RetryAfterHelper.ParseRetryAfterHeader(httpResponseMessage, _fakeClock));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(33)]
    public void RetryAfter_WhenRelativeHeaderIsFound_ShouldReturnHeaderInterval(int seconds)
    {
        using var httpResponseMessage = new HttpResponseMessage();
        httpResponseMessage.Headers.Add("Retry-After", seconds.ToString(CultureInfo.InvariantCulture));
        var interval = RetryAfterHelper.ParseRetryAfterHeader(httpResponseMessage, _fakeClock);
        Assert.Equal(TimeSpan.FromSeconds(seconds), interval);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(33)]
    public void RetryAfter_WhenAbsoluteHeaderIsFound_ShouldReturnHeaderInterval(int seconds)
    {
        using var httpResponseMessage = new HttpResponseMessage();
        httpResponseMessage.Headers.Add("Retry-After", (_fakeClock.GetUtcNow() + TimeSpan.FromSeconds(seconds)).ToString("r", CultureInfo.InvariantCulture));
        var interval = RetryAfterHelper.ParseRetryAfterHeader(httpResponseMessage, _fakeClock);
        Assert.Equal(TimeSpan.FromSeconds(seconds), interval);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using FluentAssertions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging;

public class HttpClientHedgingResiliencePredicatesTests
{
    [Fact]
    public void IsTransientException_Ok()
    {
        Assert.True(HttpClientHedgingResiliencePredicates.IsTransientHttpException(new TimeoutRejectedException()));
        Assert.True(HttpClientHedgingResiliencePredicates.IsTransientHttpException(new BrokenCircuitException()));
        Assert.True(HttpClientHedgingResiliencePredicates.IsTransientHttpException(new HttpRequestException()));
        Assert.False(HttpClientHedgingResiliencePredicates.IsTransientHttpException(new InvalidOperationException()));
    }

    [Fact]
    public void IsTransientOutcome_Ok()
    {
        Assert.True(HttpClientHedgingResiliencePredicates.IsTransientHttpOutcome(Outcome.FromException<HttpResponseMessage>(new TimeoutRejectedException())));
        Assert.True(HttpClientHedgingResiliencePredicates.IsTransientHttpOutcome(Outcome.FromException<HttpResponseMessage>(new BrokenCircuitException())));
        Assert.True(HttpClientHedgingResiliencePredicates.IsTransientHttpOutcome(Outcome.FromException<HttpResponseMessage>(new HttpRequestException())));
        Assert.False(HttpClientHedgingResiliencePredicates.IsTransientHttpOutcome(Outcome.FromException<HttpResponseMessage>(new InvalidOperationException())));
        Assert.False(HttpClientHedgingResiliencePredicates.IsTransientHttpOutcome(Outcome.FromResult<HttpResponseMessage>(null)));
    }

    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.OK, false)]
    [Theory]
    public void IsTransientOutcome_Response_Ok(HttpStatusCode code, bool expected)
    {
        using var response = new HttpResponseMessage(code);
        HttpClientHedgingResiliencePredicates.IsTransientHttpOutcome(Outcome.FromResult(response)).Should().Be(expected);
    }
}

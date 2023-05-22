// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
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
}

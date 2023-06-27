// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Polly;

public class HttpCircuitBreakerStrategyOptionsTests
{
#pragma warning disable S2330
    public static readonly IEnumerable<object[]> HandledExceptionsClassified = new[]
    {
        new object[] { new InvalidCastException(), false },
        new object[] { new HttpRequestException(), true },
        new object[] { new TaskCanceledException(), false },
        new object[] { new TimeoutRejectedException(), true },
    };

    private readonly HttpCircuitBreakerStrategyOptions _testObject;
    private readonly ResilienceContext _context;

    public HttpCircuitBreakerStrategyOptionsTests()
    {
        _testObject = new HttpCircuitBreakerStrategyOptions();
        _context = ResilienceContext.Get();
    }

    [Fact]
    public void Ctor_Defaults()
    {
        _testObject.BreakDuration.Should().Be(TimeSpan.FromSeconds(5));
        _testObject.FailureThreshold.Should().Be(0.1);
        _testObject.SamplingDuration.Should().Be(TimeSpan.FromSeconds(30));
        _testObject.MinimumThroughput.Should().Be(100);
        _testObject.ShouldHandle.Should().NotBeNull();
        _testObject.OnClosed.Should().BeNull();
        _testObject.OnOpened.Should().BeNull();
        _testObject.OnHalfOpened.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, false)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported, true)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    public async Task ShouldHandleResultAsError_DefaultValue_ShouldClassify(HttpStatusCode statusCode, bool expectedCondition)
    {
        using var response = new HttpResponseMessage { StatusCode = statusCode };
        var isTransientFailure = await _testObject.ShouldHandle(CreateArgs(response));
        Assert.Equal(expectedCondition, isTransientFailure);
    }

    [Theory]
    [MemberData(nameof(HandledExceptionsClassified))]
    public async Task ShouldHandleException_DefaultValue_ShouldClassify(Exception exception, bool expectedToHandle)
    {
        var shouldHandle = await _testObject.ShouldHandle(CreateArgs(exception));
        Assert.Equal(expectedToHandle, shouldHandle);
    }

    private OutcomeArguments<HttpResponseMessage, CircuitBreakerPredicateArguments> CreateArgs(Exception error)
        => new(_context, Outcome.FromException<HttpResponseMessage>(error), new CircuitBreakerPredicateArguments());

    private OutcomeArguments<HttpResponseMessage, CircuitBreakerPredicateArguments> CreateArgs(HttpResponseMessage response)
        => new(_context, Outcome.FromResult(response), new CircuitBreakerPredicateArguments());

}

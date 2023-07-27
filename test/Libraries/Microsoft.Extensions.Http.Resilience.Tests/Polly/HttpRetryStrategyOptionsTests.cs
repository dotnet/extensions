// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FluentAssertions;
using Polly;
using Polly.Retry;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Polly;

public class HttpRetryStrategyOptionsTests
{
#pragma warning disable S2330
    public static readonly IEnumerable<object[]> HandledExceptionsClassified = new[]
    {
        new object[] { new InvalidCastException(), false },
        new object[] { new HttpRequestException(), true }
    };

    private readonly HttpRetryStrategyOptions _testClass;

    public HttpRetryStrategyOptionsTests()
    {
        _testClass = new HttpRetryStrategyOptions();
    }

    [Fact]
    public void Ctor_Defaults()
    {
        var options = new HttpRetryStrategyOptions();

        options.BackoffType.Should().Be(RetryBackoffType.ExponentialWithJitter);
        options.RetryCount.Should().Be(3);
        options.BaseDelay.Should().Be(TimeSpan.FromSeconds(2));
        options.ShouldHandle.Should().NotBeNull();
        options.OnRetry.Should().BeNull();
        options.ShouldRetryAfterHeader.Should().BeTrue();
        options.RetryDelayGenerator.Should().NotBeNull();
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
        var response = new HttpResponseMessage { StatusCode = statusCode };
        var isTransientFailure = await _testClass.ShouldHandle(CreateArgs(Outcome.FromResult(response)));

        Assert.Equal(expectedCondition, isTransientFailure);
        response.Dispose();
    }

    [Theory]
    [MemberData(nameof(HandledExceptionsClassified))]
    public async Task ShouldHandleException_DefaultValue_ShouldClassify(Exception exception, bool expectedToHandle)
    {
        var shouldHandle = await _testClass.ShouldHandle(CreateArgs(Outcome.FromException<HttpResponseMessage>(exception)));
        Assert.Equal(expectedToHandle, shouldHandle);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, false)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported, true)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    public async Task ShouldHandleResultAsError_DefaultInstance_ShouldClassify(HttpStatusCode statusCode, bool expectedCondition)
    {
        var response = new HttpResponseMessage { StatusCode = statusCode };
        var isTransientFailure = await new HttpRetryStrategyOptions().ShouldHandle(CreateArgs(Outcome.FromResult(response)));
        Assert.Equal(expectedCondition, isTransientFailure);
        response.Dispose();
    }

    [Theory]
    [MemberData(nameof(HandledExceptionsClassified))]
    public async Task ShouldHandleException_DefaultInstance_ShouldClassify(Exception exception, bool expectedToHandle)
    {
        var shouldHandle = await new HttpRetryStrategyOptions().ShouldHandle(CreateArgs(Outcome.FromException<HttpResponseMessage>(exception)));
        Assert.Equal(expectedToHandle, shouldHandle);
    }

    [Fact]
    public async Task ShouldRetryAfterHeader_InvalidOutcomes_ShouldReturnZero()
    {
        var options = new HttpRetryStrategyOptions { ShouldRetryAfterHeader = true };
        using var responseMessage = new HttpResponseMessage { };

        Assert.NotNull(options.RetryDelayGenerator);

        var result = await options.RetryDelayGenerator(
            new(ResilienceContextPool.Shared.Get(),
            Outcome.FromResult(responseMessage),
            new RetryDelayArguments(0, TimeSpan.Zero)));
        Assert.Equal(result, TimeSpan.Zero);

        result = await options.RetryDelayGenerator(
            new(ResilienceContextPool.Shared.Get(),
            Outcome.FromResult<HttpResponseMessage>(null),
            new RetryDelayArguments(0, TimeSpan.Zero)));
        Assert.Equal(result, TimeSpan.Zero);

        result = await options.RetryDelayGenerator(
            new(ResilienceContextPool.Shared.Get(),
            Outcome.FromException<HttpResponseMessage>(new InvalidOperationException()),
            new RetryDelayArguments(0, TimeSpan.Zero)));
        Assert.Equal(result, TimeSpan.Zero);
    }

    [Fact]
    public async Task ShouldRetryAfterHeader_WhenResponseContainsRetryAfterHeader_ShouldReturnTimeSpan()
    {
        var options = new HttpRetryStrategyOptions { ShouldRetryAfterHeader = true };
        using var responseMessage = new HttpResponseMessage
        {
            Headers =
            {
                RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10))
            }
        };

        var result = await options.RetryDelayGenerator!(
            new(ResilienceContextPool.Shared.Get(),
            Outcome.FromResult(responseMessage),
            new RetryDelayArguments(0, TimeSpan.Zero)));

        Assert.Equal(result, TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetDelayGenerator_ShouldGetBasedOnShouldRetryAfterHeader(bool shouldRetryAfterHeader)
    {
        var options = new HttpRetryStrategyOptions
        {
            ShouldRetryAfterHeader = shouldRetryAfterHeader
        };

        Assert.Equal(shouldRetryAfterHeader, options.RetryDelayGenerator != null);
    }

    private static OutcomeArguments<HttpResponseMessage, RetryPredicateArguments> CreateArgs(Outcome<HttpResponseMessage> outcome)
        => new(ResilienceContextPool.Shared.Get(), outcome, new RetryPredicateArguments(0));

}

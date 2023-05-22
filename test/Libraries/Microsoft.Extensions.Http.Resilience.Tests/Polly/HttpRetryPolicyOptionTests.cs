// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Polly;

public class HttpRetryPolicyOptionTests
{
#pragma warning disable S2330
    public static readonly IEnumerable<object[]> HandledExceptionsClassified = new[]
    {
        new object[] { new InvalidCastException(), false },
        new object[] { new HttpRequestException(), true }
    };

    private readonly HttpRetryPolicyOptions _testClass;

    public HttpRetryPolicyOptionTests()
    {
        _testClass = new HttpRetryPolicyOptions();
    }

    [Fact]
    public void ShouldHandleResultAsError_ShouldGetAndSet()
    {
        Predicate<HttpResponseMessage> testValue = response => !response.IsSuccessStatusCode;
        _testClass.ShouldHandleResultAsError = testValue;

        Assert.Equal(testValue, _testClass.ShouldHandleResultAsError);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, false)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported, true)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    public void ShouldHandleResultAsError_DefaultValue_ShouldClassify(HttpStatusCode statusCode, bool expectedCondition)
    {
        var response = new HttpResponseMessage { StatusCode = statusCode };
        var isTransientFailure = _testClass.ShouldHandleResultAsError(response);
        Assert.Equal(expectedCondition, isTransientFailure);
        response.Dispose();
    }

    [Fact]
    public void ShouldHandleException_ShouldGetAndSet()
    {
        Predicate<Exception> testValue = ex => ex is ArgumentNullException;
        _testClass.ShouldHandleException = testValue;

        Assert.Equal(testValue, _testClass.ShouldHandleException);
    }

    [Theory]
    [MemberData(nameof(HandledExceptionsClassified))]
    public void ShouldHandleException_DefaultValue_ShouldClassify(Exception exception, bool expectedToHandle)
    {
        var shouldHandle = _testClass.ShouldHandleException(exception);
        Assert.Equal(expectedToHandle, shouldHandle);
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, false)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported, true)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    public void ShouldHandleResultAsError_DefaultInstance_ShouldClassify(HttpStatusCode statusCode, bool expectedCondition)
    {
        var response = new HttpResponseMessage { StatusCode = statusCode };
        var isTransientFailure = new HttpRetryPolicyOptions().ShouldHandleResultAsError(response);
        Assert.Equal(expectedCondition, isTransientFailure);
        response.Dispose();
    }

    [Theory]
    [MemberData(nameof(HandledExceptionsClassified))]
    public void ShouldHandleException_DefaultInstance_ShouldClassify(Exception exception, bool expectedToHandle)
    {
        var shouldHandle = new HttpRetryPolicyOptions().ShouldHandleException(exception);
        Assert.Equal(expectedToHandle, shouldHandle);
    }

    [Fact]
    public void ShouldRetryAfterHeader_WhenNullHeader_ShouldReturnZero()
    {
        var options = new HttpRetryPolicyOptions { ShouldRetryAfterHeader = true };
        using var responseMessage = new HttpResponseMessage { };
        var delegateResult = new DelegateResult<HttpResponseMessage>(responseMessage);
        var result = options.RetryDelayGenerator != null
            ? options.RetryDelayGenerator(
                new RetryDelayArguments<HttpResponseMessage>(delegateResult, new Context(), CancellationToken.None))
            : TimeSpan.Zero;
        Assert.Equal(result, TimeSpan.Zero);
    }

    [Fact]
    public void ShouldRetryAfterHeader_WhenResponseContainsRetryAfterHeader_ShouldReturnTimeSpan()
    {
        var options = new HttpRetryPolicyOptions { ShouldRetryAfterHeader = true };
        using var responseMessage = new HttpResponseMessage
        {
            Headers =
                {
                    RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10))
                }
        };
        var args = new RetryDelayArguments<HttpResponseMessage>(
            new DelegateResult<HttpResponseMessage>(responseMessage),
            new Context(),
            CancellationToken.None);

        var result = options.RetryDelayGenerator != null ? options.RetryDelayGenerator(args) : TimeSpan.Zero;
        Assert.Equal(result, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void ShouldRetryAfterHeader_WhenResponseContainsNullHeader_ShouldReturnZero()
    {
        var options = new HttpRetryPolicyOptions { ShouldRetryAfterHeader = true };
        using var responseMessage = new HttpResponseMessage
        {
        };
        var delegateResult = new DelegateResult<HttpResponseMessage>(responseMessage);
        var result = options.RetryDelayGenerator != null
            ? options.RetryDelayGenerator(
                new RetryDelayArguments<HttpResponseMessage>(delegateResult, new Context(), CancellationToken.None))
            : TimeSpan.Zero;
        Assert.Equal(result, TimeSpan.Zero);

        result = options.RetryDelayGenerator != null ? options.RetryDelayGenerator(
            new RetryDelayArguments<HttpResponseMessage>(null!, new Context(), CancellationToken.None))
        : TimeSpan.Zero;
        Assert.Equal(result, TimeSpan.Zero);
    }

    [Fact]
    public void ShouldRetryAfterHeader_WhenDelegateHasException_ShouldReturnZero()
    {
        var options = new HttpRetryPolicyOptions { ShouldRetryAfterHeader = true };
        var args = new RetryDelayArguments<HttpResponseMessage>(
            new DelegateResult<HttpResponseMessage>(new ArgumentNullException()),
            new Context(),
            CancellationToken.None);

        var result = options.RetryDelayGenerator!(args);
        Assert.Equal(result, TimeSpan.Zero);
    }

    [Fact]
    public void ShouldRetryAfterHeader_WhenHeaderSetUsingAdd_ShouldReturnTimeSpan()
    {
        var options = new HttpRetryPolicyOptions { ShouldRetryAfterHeader = true };
        using var responseMessage = new HttpResponseMessage();
        responseMessage.Headers.Add("Retry-After", "10");
        var args = new RetryDelayArguments<HttpResponseMessage>(
            new DelegateResult<HttpResponseMessage>(responseMessage),
            new Context(),
            CancellationToken.None);

        var result = options.RetryDelayGenerator != null ? options.RetryDelayGenerator(args) : TimeSpan.Zero;
        Assert.Equal(result, TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetDelayGenerator_ShouldGetBasedOnShouldRetryAfterHeader(bool shouldRetryAfterHeader)
    {
        var options = new HttpRetryPolicyOptions
        {
            ShouldRetryAfterHeader = shouldRetryAfterHeader
        };

        Assert.Equal(shouldRetryAfterHeader, options.RetryDelayGenerator != null);
    }
}

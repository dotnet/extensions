// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Polly.Timeout;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Polly;

public class HttpCircuitBreakerPolicyOptionsTests
{
#pragma warning disable S2330
    public static readonly IEnumerable<object[]> HandledExceptionsClassified = new[]
    {
        new object[] { new InvalidCastException(), false },
        new object[] { new HttpRequestException(), true },
        new object[] { new TaskCanceledException(), false },
        new object[] { new TimeoutRejectedException(), true },
    };

    private readonly HttpCircuitBreakerPolicyOptions _testObject;

    public HttpCircuitBreakerPolicyOptionsTests()
    {
        _testObject = new HttpCircuitBreakerPolicyOptions();
    }

    [Fact]
    public void Constructor_ShouldInitialize()
    {
        var instance = new HttpCircuitBreakerPolicyOptions();
        Assert.NotNull(instance);
    }

    [Fact]
    public void ShouldHandleResultAsError_ShouldGetAndSet()
    {
        Predicate<HttpResponseMessage> testValue = response => !response.IsSuccessStatusCode;
        _testObject.ShouldHandleResultAsError = testValue;

        Assert.Equal(testValue, _testObject.ShouldHandleResultAsError);
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
        var isTransientFailure = _testObject.ShouldHandleResultAsError(response);
        Assert.Equal(expectedCondition, isTransientFailure);
        response.Dispose();
    }

    [Fact]
    public void ShouldHandleException_ShouldGetAndSet()
    {
        Predicate<Exception> testValue = ex => ex is ArgumentNullException;
        _testObject.ShouldHandleException = testValue;

        Assert.Equal(testValue, _testObject.ShouldHandleException);
    }

    [Theory]
    [MemberData(nameof(HandledExceptionsClassified))]
    public void ShouldHandleException_DefaultValue_ShouldClassify(Exception exception, bool expectedToHandle)
    {
        var shouldHandle = _testObject.ShouldHandleException(exception);
        Assert.Equal(expectedToHandle, shouldHandle);
    }

    [Fact]
    public void OnCircuitBreak_ShouldGetAndSet()
    {
        Action<BreakActionArguments<HttpResponseMessage>> testValue = _ => { };
        _testObject.OnCircuitBreak = testValue;

        Assert.Equal(testValue, _testObject.OnCircuitBreak);
    }

    [Fact]
    public void OnCircuitReset_ShouldGetAndSet()
    {
        Action<ResetActionArguments> testValue = _ => { };
        _testObject.OnCircuitReset = testValue;

        Assert.Equal(testValue, _testObject.OnCircuitReset);
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
        var isTransientFailure = new HttpCircuitBreakerPolicyOptions().ShouldHandleResultAsError(response);
        Assert.Equal(expectedCondition, isTransientFailure);
        response.Dispose();
    }

    [Theory]
    [MemberData(nameof(HandledExceptionsClassified))]
    public void ShouldHandleException_DefaultInstance_ShouldClassify(Exception exception, bool expectedToHandle)
    {
        var shouldHandle = new HttpCircuitBreakerPolicyOptions().ShouldHandleException(exception);
        Assert.Equal(expectedToHandle, shouldHandle);
    }

    [Fact]
    public void OnCircuitBreak_NoOp()
    {
        var options = new HttpCircuitBreakerPolicyOptions();
        var context = new Context();
        var expectedError = "Something went wrong";
        var delegateResult = new DelegateResult<HttpResponseMessage>(new InvalidOperationException(expectedError));
        var args = new BreakActionArguments<HttpResponseMessage>(
            delegateResult,
            context,
            TimeSpan.FromSeconds(2),
            CancellationToken.None);
        Assert.Null(Record.Exception(() => options.OnCircuitBreak(args)));
    }

    [Fact]
    public void OnCircuitReset_NoOp()
    {
        var options = new HttpCircuitBreakerPolicyOptions { OnCircuitReset = (_) => { } };
        var context = new Context();

        Assert.Null(Record.Exception(() => options.OnCircuitReset(new ResetActionArguments(context, CancellationToken.None))));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.Resilience.Options;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Polly;

public class HttpFallbackPolicyOptionsTests
{
#pragma warning disable S2330
    public static readonly IEnumerable<object[]> HandledExceptionsClassified = new[]
    {
        new object[] { new InvalidCastException(), false },
        new object[] { new HttpRequestException(), true }
    };

    [Theory]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.RequestEntityTooLarge, false)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.HttpVersionNotSupported, true)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    public void ShouldHandleResultAsError_DefaultInstance_ShouldClassify(
        HttpStatusCode statusCode,
        bool expected)
    {
        using var httpReq = new HttpResponseMessage { StatusCode = statusCode };
        var actual = new HttpFallbackPolicyOptions().ShouldHandleResultAsError(httpReq);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(HandledExceptionsClassified))]
    public void ShouldHandleException_DefaultInstance_ShouldClassify(
        Exception exception,
        bool expected)
    {
        var actual = new HttpFallbackPolicyOptions().ShouldHandleException(exception);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OnFallback_NoOp()
    {
        var options = new HttpFallbackPolicyOptions();
        var context = new Context();
        var expectedError = "Something went wrong";
        var delegateResult = new DelegateResult<HttpResponseMessage>(new InvalidOperationException(expectedError));

        var task = options.OnFallbackAsync(new FallbackTaskArguments<HttpResponseMessage>(delegateResult, context, CancellationToken.None));
        Assert.NotNull(task);
    }
}

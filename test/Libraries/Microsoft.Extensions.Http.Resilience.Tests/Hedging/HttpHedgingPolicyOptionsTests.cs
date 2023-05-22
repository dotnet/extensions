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

namespace Microsoft.Extensions.Http.Resilience.Test.Hedging;

public class HttpHedgingPolicyOptionsTests
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
        var actual = new HttpHedgingPolicyOptions().ShouldHandleResultAsError(httpReq);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [MemberData(nameof(HandledExceptionsClassified))]
    public void ShouldHandleException_DefaultInstance_ShouldClassify(
        Exception exception,
        bool expected)
    {
        var actual = new HttpHedgingPolicyOptions().ShouldHandleException(exception);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void OnHedging_CallsRecordMetric()
    {
        var options = new HttpHedgingPolicyOptions();
        var expectedError = "Something went wrong";
        var delegateResult = new DelegateResult<HttpResponseMessage>(new InvalidOperationException(expectedError));

        Assert.NotNull(options.OnHedgingAsync(
            new HedgingTaskArguments<HttpResponseMessage>(
                delegateResult,
                new Context(),
                0,
                CancellationToken.None)));
    }
}

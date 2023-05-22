// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Resilience.Internal;
using Polly;
using Xunit;

namespace Microsoft.Extensions.Resilience.Polly.Test;

public class FailureReasonResolverTest
{
    [Fact]
    public void GetFailureReason_WhenExceptionIsThrown_ShouldReturnException()
    {
        const string ExpectedMessage = "Cannot access year 2020. This part of the memory is under quarantine.";
        var result = new DelegateResult<string>(new InvalidOperationException(ExpectedMessage));
        var failureReason = FailureReasonResolver.GetFailureReason(result);
        Assert.Equal($"Error: {ExpectedMessage}", failureReason);
    }

    [Fact]
    public void GetFailureReason_GetFailureMessageFromException()
    {
        const string ExpectedMessage = "Cannot access year 2020. This part of the memory is under quarantine.";
        var exception = new InvalidOperationException(ExpectedMessage);
        var failureReason = FailureReasonResolver.GetFailureFromException(exception);
        Assert.Equal($"Error: {ExpectedMessage}", failureReason);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ʕ•́ᴥ•̀ʔ")]
    [InlineData("We wish you a Merry Christmas")]
    public void GetFailureReason_NullResult_ShouldReturnUndefined(string resultContent)
    {
        var result = new DelegateResult<string>(resultContent);
        var failureReason = FailureReasonResolver.GetFailureReason(result);
        Assert.Equal("Undefined", failureReason);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "Status code: BadRequest")]
    [InlineData(HttpStatusCode.InternalServerError, "Status code: InternalServerError")]
    public void GetFailureReason_WhenErrorStatus_ShouldReturnException(HttpStatusCode code, string expectedFailure)
    {
        using var responseMessage = new HttpResponseMessage { StatusCode = code };
        var httpResponseDelegate = new DelegateResult<HttpResponseMessage>(responseMessage);

        var failureReason = FailureReasonResolver.GetFailureReason(httpResponseDelegate);
        Assert.Equal(expectedFailure, failureReason);
    }
}

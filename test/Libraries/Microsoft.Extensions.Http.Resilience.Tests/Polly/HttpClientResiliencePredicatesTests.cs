// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Polly;
using Polly.Timeout;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test.Polly;

public class HttpClientResiliencePredicatesTests
{
#pragma warning disable S2330 // Array covariance should not be used
    public static readonly IEnumerable<object[]> HandledExceptionsClassified = new[]
    {
        new object[] { new InvalidCastException(), false },
        new object[] { new HttpRequestException(), true },
        new object[] { new TimeoutRejectedException(), true },
    };
#pragma warning restore S2330 // Array covariance should not be used

    [Fact]
    public void IsTransientHttpException_NullException_ShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HttpClientResiliencePredicates.IsTransientHttpException(null!));
    }

    [Theory]
    [MemberData(nameof(HandledExceptionsClassified))]
    public void IsTransientHttpException_Exception_ShouldClassify(Exception ex, bool expectedCondition)
    {
        var isTransientHttpException = HttpClientResiliencePredicates.IsTransientHttpException(ex);
        Assert.Equal(expectedCondition, isTransientHttpException);

        isTransientHttpException = HttpClientResiliencePredicates.IsTransient(Outcome.FromException<HttpResponseMessage>(ex));
        Assert.Equal(expectedCondition, isTransientHttpException);
    }

    public class HttpResponseMessageExtensionsTests
    {
        [Theory]
        [InlineData(HttpStatusCode.OK, false)]
        [InlineData(HttpStatusCode.BadRequest, false)]
        [InlineData(HttpStatusCode.RequestEntityTooLarge, false)]
        [InlineData(HttpStatusCode.InternalServerError, true)]
        [InlineData(HttpStatusCode.HttpVersionNotSupported, true)]
        [InlineData(HttpStatusCode.RequestTimeout, true)]
        [InlineData((HttpStatusCode)429, true)]
        public void IsTransientFailure_ShouldClassify(HttpStatusCode statusCode, bool expectedCondition)
        {
            var response = new HttpResponseMessage { StatusCode = statusCode };
            var isTransientFailure = HttpClientResiliencePredicates.IsTransientHttpFailure(response);
            Assert.Equal(expectedCondition, isTransientFailure);

            isTransientFailure = HttpClientResiliencePredicates.IsTransient(Outcome.FromResult(response));
            Assert.Equal(expectedCondition, isTransientFailure);

            response.Dispose();
        }

        [Fact]
        public void IsTransientFailure_NullResponse_ShouldThrow()
        {
            Assert.Throws<ArgumentNullException>(() => HttpClientResiliencePredicates.IsTransientHttpFailure(null!));
        }
    }
}

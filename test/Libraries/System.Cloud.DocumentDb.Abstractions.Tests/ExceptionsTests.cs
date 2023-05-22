// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using FluentAssertions;
using Xunit;

namespace System.Cloud.DocumentDb.Test;

public class ExceptionsTests
{
    private const string TestMessage = "message";

    private const int TestStatus = (int)HttpStatusCode.Accepted;
    private const int DefaultStatus = (int)HttpStatusCode.InternalServerError;

    private readonly TimeSpan _testTime = TimeSpan.MaxValue;
    private readonly TimeSpan _defaultTime = TimeSpan.Zero;

    private readonly RequestInfo _requestInfo = new("region", "table", 5);

    private readonly Exception _testException = new DatabaseException("test");

    [Fact]
    public void TestConstructors()
    {
        VerifyException(new DatabaseException(
            TestMessage, _testException, TestStatus, 0, _requestInfo),
            TestMessage, _testException, TestStatus, _requestInfo);
        VerifyException(new DatabaseServerException(
            TestMessage, _testException, TestStatus, 0, _requestInfo),
            TestMessage, _testException, TestStatus, _requestInfo);
        VerifyRetryableException(new DatabaseRetryableException(
            TestMessage, _testException, TestStatus, 0, _testTime, _requestInfo),
            TestMessage, _testException, TestStatus, _testTime, _requestInfo);

        VerifyException(new DatabaseException(
            TestMessage, _testException),
            TestMessage, _testException, DefaultStatus, default);
        VerifyException(new DatabaseClientException(
            TestMessage, _testException),
            TestMessage, _testException, DefaultStatus, default);
        VerifyException(new DatabaseServerException(
            TestMessage, _testException),
            TestMessage, _testException, DefaultStatus, default);
        VerifyRetryableException(new DatabaseRetryableException(
            TestMessage, _testException),
            TestMessage, _testException, DefaultStatus, _defaultTime, default);

        VerifyException(new DatabaseException(
            TestMessage, DefaultStatus, 0, _requestInfo),
            TestMessage, null, DefaultStatus, _requestInfo);
        VerifyException(new DatabaseServerException(
            TestMessage, DefaultStatus, 0, _requestInfo),
            TestMessage, null, DefaultStatus, _requestInfo);

        VerifyException(new DatabaseException(
            TestMessage),
            TestMessage, null, DefaultStatus, default);
        VerifyException(new DatabaseClientException(
            TestMessage),
            TestMessage, null, DefaultStatus, default);
        VerifyException(new DatabaseServerException(
            TestMessage),
            TestMessage, null, DefaultStatus, default);
        VerifyRetryableException(new DatabaseRetryableException(
            TestMessage),
            TestMessage, null, DefaultStatus, _defaultTime, default);

        VerifyException(new DatabaseException(),
            null, null, DefaultStatus, default);
        VerifyException(new DatabaseClientException(),
            null, null, DefaultStatus, default);
        VerifyException(new DatabaseServerException(),
            null, null, DefaultStatus, default);
        VerifyRetryableException(new DatabaseRetryableException(),
            null, null, DefaultStatus, _defaultTime, default);
    }

    private static void VerifyException(DatabaseException exception,
        string? testMessage, Exception? testException, int? httpStatusCode, RequestInfo info)
    {
        Assert.Equal(testMessage ?? $"Exception of type '{exception.GetType().FullName}' was thrown.", exception.Message);
        Assert.Equal(testException, exception.InnerException);
        Assert.Equal((HttpStatusCode?)httpStatusCode, exception.HttpStatusCode);
        Assert.Equal(0, exception.SubStatusCode);
        exception.RequestInfo.Should().Be(info);
    }

    private static void VerifyRetryableException(DatabaseRetryableException exception,
        string? testMessage, Exception? testException, int httpStatusCode,
        TimeSpan testTime, RequestInfo info)
    {
        Assert.Equal(testMessage ?? $"Exception of type '{exception.GetType().FullName}' was thrown.", exception.Message);
        Assert.Equal(testException, exception.InnerException);
        Assert.Equal((HttpStatusCode)httpStatusCode, exception.HttpStatusCode);
        Assert.Equal(0, exception.SubStatusCode);
        Assert.Equal(testTime, exception.RetryAfter);
        exception.RequestInfo.Should().Be(info);
    }
}

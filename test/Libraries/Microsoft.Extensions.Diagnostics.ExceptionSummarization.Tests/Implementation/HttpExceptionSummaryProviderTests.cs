// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization.Tests;

public class HttpExceptionSummaryProviderTests
{
    private const int DefaultDescriptionIndex = -1;
    private readonly HttpExceptionSummaryProvider _exceptionSummaryProvider = new();

    public static IEnumerable<object[]> SocketErrors()
    {
        foreach (var socketError in Enum.GetValues(typeof(SocketError)))
        {
            if (socketError != null)
            {
                yield return new[] { socketError };
            }
        }
    }

    public static IEnumerable<object[]> WebExceptionStatuses()
    {
        foreach (var webException in Enum.GetValues(typeof(WebExceptionStatus)))
        {
            if (webException != null)
            {
                yield return new[] { webException };
            }
        }
    }

    [Fact]
    public void Describe_WithNullException_ThrowsArgumentNullException()
    {
        Exception? ex = null!;
        Assert.ThrowsAny<ArgumentNullException>(() =>
        _exceptionSummaryProvider.Describe(ex, out var additionalDetails));
    }

    [Fact]
    public void SupportedExceptionTypes_ContainsIntendedHttpExceptions()
    {
        var httpExceptionTypes = new[]
        {
            typeof(TaskCanceledException),
            typeof(OperationCanceledException),
            typeof(WebException),
            typeof(SocketException)
        };

        Assert.Equal(httpExceptionTypes, _exceptionSummaryProvider.SupportedExceptionTypes);
        Assert.Contains(typeof(SocketException), _exceptionSummaryProvider.SupportedExceptionTypes);
        Assert.Contains(typeof(WebException), _exceptionSummaryProvider.SupportedExceptionTypes);
        Assert.Contains(typeof(OperationCanceledException), _exceptionSummaryProvider.SupportedExceptionTypes);
    }

    [Theory]
    [MemberData(nameof(WebExceptionStatuses))]
    public void Describe_WithKnownWebException_ReturnDetails(WebExceptionStatus webExceptionStatus)
    {
        Exception exception = new WebException("test", webExceptionStatus);
        var descriptionIndex = _exceptionSummaryProvider
            .Describe(exception, out var additionalDetails);

        Assert.Equal(webExceptionStatus.ToString(), _exceptionSummaryProvider.Descriptions[descriptionIndex]);
        Assert.Null(additionalDetails);
    }

    [Fact]
    public void Describe_WithUnknownWebExceptionStatus_ReturnDefaultDetails()
    {
        var exception = new WebException("test", (WebExceptionStatus)12345);
        var descriptionIndex = _exceptionSummaryProvider
            .Describe(exception, out var additionalDetails);

        Assert.Equal(DefaultDescriptionIndex, descriptionIndex);
        Assert.Null(additionalDetails);
    }

    [Theory]
    [MemberData(nameof(SocketErrors))]
    public void Describe_WithKnownSocketException_ReturnDetails(SocketError socketError)
    {
        Exception exception = new SocketException((int)socketError);
        var descriptionIndex = _exceptionSummaryProvider.Describe(exception, out var additionalDetails);

        Assert.Equal(socketError.ToString(), _exceptionSummaryProvider.Descriptions[descriptionIndex]);
        Assert.Null(additionalDetails);
    }

    [Fact]
    public void Describe_WithUnknownSocketError_ReturnDefaultDetails()
    {
        var errorCode = -2;
        Exception exception = new SocketException(errorCode);

        var descriptionIndex = _exceptionSummaryProvider
            .Describe(exception, out var additionalDetails);

        Assert.Equal(DefaultDescriptionIndex, descriptionIndex);
        Assert.Null(additionalDetails);
    }

    [Fact]
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Intended.")]
    public async Task Describe_WithTaskCanceledException_ReturnDetails()
    {
        try
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.Zero);
            tokenSource.Cancel();
            var task = Task.Run(() => { }, tokenSource.Token);

            await task;
        }
        catch (Exception exception)
        {
            var descriptionIndex = _exceptionSummaryProvider.Describe(exception, out var additionalDetails);

            Assert.Equal("TaskCanceled", _exceptionSummaryProvider.Descriptions[descriptionIndex]);
            Assert.Null(additionalDetails);
        }

        Exception exception2 = new TaskCanceledException();

        var descriptionIndex2 = _exceptionSummaryProvider.Describe(exception2, out var additionalDetails2);

        Assert.Equal("TaskTimeout", _exceptionSummaryProvider.Descriptions[descriptionIndex2]);
        Assert.Null(additionalDetails2);
    }

    [Fact]
    public void Describe_WithUnknownException_ReturnDefaultDetails()
    {
        Exception exception = new ArgumentException("This is not an exception that HttpExceptionProvider understands");

        var descriptionIndex = _exceptionSummaryProvider
            .Describe(exception, out var additionalDetails);

        Assert.Equal(DefaultDescriptionIndex, descriptionIndex);
        Assert.Null(additionalDetails);
    }
}

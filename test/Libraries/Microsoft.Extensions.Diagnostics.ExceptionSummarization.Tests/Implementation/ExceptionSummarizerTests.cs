// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Microsoft.Extensions.Diagnostics.ExceptionSummarization.Tests;

public class ExceptionSummarizerTests
{
    private const string DefaultDescription = "Unknown";
    private const string AdditionalDetails = "Some additional details";
    private static readonly List<string> _httpDescriptions = new List<string> { "TaskTimeout", "TaskCanceled" }
        .Concat(Enum.GetNames(typeof(WebExceptionStatus)).ToList())
        .Concat(Enum.GetNames(typeof(SocketError)).ToList()).ToList();
    private static readonly List<Type> _httpSupportedExceptionTypes = new()
    {
        typeof(TaskCanceledException),
        typeof(OperationCanceledException),
        typeof(WebException),
        typeof(SocketException),
    };

    private readonly Mock<IExceptionSummaryProvider> _httpExceptionProviderMock;
    private readonly IExceptionSummarizer _exceptionSummarizer;

    public ExceptionSummarizerTests()
    {
        _httpExceptionProviderMock = new Mock<IExceptionSummaryProvider>(MockBehavior.Strict);
        _httpExceptionProviderMock.Setup(mock => mock.SupportedExceptionTypes).Returns(_httpSupportedExceptionTypes);
        _httpExceptionProviderMock.Setup(mock => mock.Descriptions).Returns(_httpDescriptions);

        _exceptionSummarizer = new ExceptionSummarizer(new List<IExceptionSummaryProvider> { _httpExceptionProviderMock.Object });
    }

    [Fact]
    public void Summarize_WithProviderSummaryAndInvalidIndex_ReturnSummary()
    {
        var exception = new WebException("test", WebExceptionStatus.RequestCanceled);
        var descriptionIndex = 1000;
        var additionalDetails = $"Exception summary provider {_httpExceptionProviderMock.Object.GetType().Name} returned invalid short description index {descriptionIndex}";
        var exceptionSummary = new ExceptionSummary("WebException", DefaultDescription, additionalDetails);

        _httpExceptionProviderMock
            .Setup(mock => mock.Describe(exception, out additionalDetails)).Returns(1000);

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(exceptionSummary, summary);
        Assert.Equal(exceptionSummary.ToString(), summary.ToString());
        Assert.Equal(exceptionSummary.AdditionalDetails, summary.AdditionalDetails);
    }

    [Fact]
    public void Summarize_WithProviderSummaryAndIndexSameAsCount_ReturnSummary()
    {
        var exception = new WebException("test", WebExceptionStatus.RequestCanceled);
        var descriptionIndex = _httpDescriptions.Count;
        var additionalDetails = $"Exception summary provider {_httpExceptionProviderMock.Object.GetType().Name} returned invalid short description index {descriptionIndex}";
        var exceptionSummary = new ExceptionSummary("WebException", DefaultDescription, additionalDetails);

        _httpExceptionProviderMock
            .Setup(mock => mock.Describe(exception, out additionalDetails)).Returns(descriptionIndex);

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(exceptionSummary, summary);
        Assert.Equal(exceptionSummary.ToString(), summary.ToString());
        Assert.Equal(exceptionSummary.AdditionalDetails, summary.AdditionalDetails);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Intended.")]
    public void Summarize_WithInnerWebExceptionAndInvalidIndex_ReturnSummary()
    {
        var exception = new Exception("test", new WebException("Error", (WebExceptionStatus)30));
        var descriptionIndex = -1;
        var additionalDetails = $"Exception summary provider {_httpExceptionProviderMock.Object.GetType().Name} returned invalid short description index {descriptionIndex}";
        var innerExceptionSummary = new ExceptionSummary("Exception->WebException", DefaultDescription, additionalDetails);

        if (exception.InnerException != null)
        {
            _httpExceptionProviderMock
                .Setup(mock => mock.Describe(exception.InnerException, out additionalDetails))
                .Returns(descriptionIndex);
        }

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(innerExceptionSummary, summary);
        Assert.Equal(innerExceptionSummary.ToString(), summary.ToString());
    }

    [Fact]
    public void Summarize_WithProviderSummaryAndValidIndex_ReturnSummary()
    {
        var exception = new WebException("test", WebExceptionStatus.RequestCanceled);
        var descriptionIndex = _httpDescriptions.FindIndex(x => x.Equals(WebExceptionStatus.RequestCanceled.ToString()));
        var additionalDetails = DefaultDescription;
        var exceptionSummary = new ExceptionSummary("WebException", "RequestCanceled", additionalDetails);

        _httpExceptionProviderMock
            .Setup(mock => mock.Describe(exception, out additionalDetails))
            .Returns(descriptionIndex);

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(exceptionSummary, summary);
        Assert.Equal(exceptionSummary.ToString(), summary.ToString());
    }

    [Fact]
    public void Summarize_WithProviderSummaryAndAdditionalDetails_ReturnSummary()
    {
        var exception = new WebException("test", WebExceptionStatus.RequestCanceled);
        var descriptionIndex = _httpDescriptions.FindIndex(x => x.Equals(WebExceptionStatus.RequestCanceled.ToString()));
        var additionalDetails = AdditionalDetails;
        var exceptionSummary = new ExceptionSummary("WebException", "RequestCanceled", additionalDetails);

        _httpExceptionProviderMock
            .Setup(mock => mock.Describe(exception, out additionalDetails))
            .Returns(descriptionIndex);

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(exceptionSummary, summary);
        Assert.Equal(exceptionSummary.ToString(), summary.ToString());
    }

    [Theory]
    [InlineData(AdditionalDetails)]
    [InlineData(DefaultDescription)]
    public void Summarize_WithWebException_ReturnSummary(string? additionalDetails)
    {
        var exception = new WebException("test", WebExceptionStatus.ConnectFailure);
        var descriptionIndex = _httpDescriptions.FindIndex(x => x.Equals(WebExceptionStatus.ConnectFailure.ToString()));
        var exceptionSummary = new ExceptionSummary("WebException", "ConnectFailure", additionalDetails ?? DefaultDescription);

        _httpExceptionProviderMock
            .Setup(mock => mock.Describe(exception, out additionalDetails))
            .Returns(descriptionIndex);

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(exceptionSummary, summary);
    }

    [Theory]
    [InlineData(AdditionalDetails)]
    [InlineData(DefaultDescription)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Intended.")]
    public void Summarize_WithInnerWebException_ReturnSummary(string? additionalDetails)
    {
        var exception = new Exception("test", new WebException("test", WebExceptionStatus.RequestCanceled));
        var descriptionIndex = _httpDescriptions
            .FindIndex(x => x.Equals(WebExceptionStatus.RequestCanceled.ToString()));
        var innerExceptionSummary = new ExceptionSummary("Exception->WebException", "RequestCanceled", additionalDetails ?? DefaultDescription);

        if (exception.InnerException != null)
        {
            _httpExceptionProviderMock
                .Setup(mock => mock.Describe(exception.InnerException, out additionalDetails))
                .Returns(descriptionIndex);
        }

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(innerExceptionSummary, summary);
        Assert.Equal(innerExceptionSummary.ToString(), summary.ToString());
    }

    [Theory]
    [InlineData(AdditionalDetails)]
    [InlineData(DefaultDescription)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Intended.")]
    public void Summarize_WithInnerSocketException_ReturnSummary(string? additionalDetails)
    {
        var exception = new Exception("test", new SocketException((int)SocketError.TimedOut));
        var descriptionIndex = _httpDescriptions.FindIndex(x => x.Equals(SocketError.TimedOut.ToString()));
        var innerExceptionSummary = new ExceptionSummary("Exception->SocketException", "TimedOut", additionalDetails ?? DefaultDescription);

        if (exception.InnerException != null)
        {
            _httpExceptionProviderMock
                .Setup(mock => mock.Describe(exception.InnerException, out additionalDetails))
                .Returns(descriptionIndex);
        }

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(innerExceptionSummary, summary);
        Assert.Equal(innerExceptionSummary.ToString(), summary.ToString());
    }

    [Theory]
    [InlineData(AdditionalDetails)]
    [InlineData(DefaultDescription)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Intended.")]
    public async Task Summarize_WithInnerTaskCanceledException_ReturnSummary(string? additionalDetails)
    {
        var descriptionIndex = _httpDescriptions.FindIndex(x => x.Equals("TaskCanceled"));

        try
        {
            using var tokenSource = new CancellationTokenSource(TimeSpan.Zero);
            tokenSource.Cancel();
            var task = Task.Run(() => { }, tokenSource.Token);

            await task;
        }
        catch (TaskCanceledException ex)
        {
            var exception = new Exception("test", ex);
            var innerExceptionSummary = new ExceptionSummary("Exception->TaskCanceledException", "TaskCanceled", additionalDetails ?? DefaultDescription);

            if (exception.InnerException != null)
            {
                _httpExceptionProviderMock
                    .Setup(mock => mock.Describe(exception.InnerException, out additionalDetails))
                    .Returns(descriptionIndex);
            }

            var summary = _exceptionSummarizer.Summarize(exception);

            Assert.Equal(innerExceptionSummary, summary);
            Assert.Equal(innerExceptionSummary.ToString(), summary.ToString());
        }
    }

    [Theory]
    [InlineData(AdditionalDetails)]
    [InlineData(DefaultDescription)]
    [InlineData(null)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "Intended.")]
    public void Summarize_WithInnerTaskTimeout_ReturnSummary(string? additionalDetails)
    {
        var exception = new Exception("test", new TaskCanceledException());
        var descriptionIndex = _httpDescriptions.FindIndex(x => x.Equals("TaskTimeout"));
        var innerExceptionSummary = new ExceptionSummary("Exception->TaskCanceledException", "TaskTimeout", additionalDetails ?? DefaultDescription);

        if (exception.InnerException != null)
        {
            _httpExceptionProviderMock
                .Setup(mock => mock.Describe(exception.InnerException, out additionalDetails))
                .Returns(descriptionIndex);
        }

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(innerExceptionSummary, summary);
        Assert.Equal(innerExceptionSummary.ToString(), summary.ToString());
    }

    [Fact]
    public void Summarize_WithNotDefaultHResult_ReturnSummary()
    {
        uint resultCode = 0x80131501;
        var exception = new TestException(resultCode);
        var exceptionHResultSummary = new ExceptionSummary("TestException", "Unknown", "-2146233087");

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(exceptionHResultSummary, summary);
        Assert.Equal(exceptionHResultSummary.ToString(), summary.ToString());
    }

    [Fact]
    public void Summarize_WithDefaultHResultAndWithoutInnerException_ReturnDefaultSummary()
    {
        var exception = new TestException(0);
        var exceptionHResultSummary = new ExceptionSummary("TestException", "Unknown", "Unknown");

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(exceptionHResultSummary, summary);
        Assert.Equal(exceptionHResultSummary.ToString(), summary.ToString());
    }

    [Fact]
    public void Summarize_WithDefaultHResultAndInnerException_ReturnSummary()
    {
        var exception = new TestException(0, "Test", new TestException(0));
        var exceptionHResultSummary = new ExceptionSummary("TestException", "TestException", "Unknown");

        var summary = _exceptionSummarizer.Summarize(exception);

        Assert.Equal(exceptionHResultSummary, summary);
        Assert.Equal(exceptionHResultSummary.ToString(), summary.ToString());
    }
}

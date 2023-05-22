// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Tests.Data.Consumers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;
using Xunit;

namespace System.Cloud.Messaging.Tests.Implementations;

/// <summary>
/// Tests for <see cref="BaseMessageConsumer"/>.
/// </summary>
public class BaseMessageConsumerTests
{
    private static MessageContext CreateContext(IFeatureCollection sourceFeatures)
    {
        var context = new MessageContext(new FeatureCollection());
        context.SetMessageSourceFeatures(sourceFeatures);
        return context;
    }

    [Fact]
    public async Task BaseMessageConsumer_ShouldNotStartProcessing_WhenCancellationTokenIsExpired()
    {
        var mockMessageSource = new Mock<IMessageSource>();
        var mockMessageDelegate = new Mock<IMessageDelegate>();
        var mockLogger = new Mock<ILogger>();

        using var cts = new CancellationTokenSource();
        cts.Cancel(false);

        var messageConsumer = new DerivedConsumer(mockMessageSource.Object, mockMessageDelegate.Object, mockLogger.Object);
        await messageConsumer.ExecuteAsync(cts.Token);

        mockMessageSource.VerifyNoOtherCalls();
        mockMessageDelegate.VerifyNoOtherCalls();
        mockLogger.VerifyNoOtherCalls();
    }

    [Fact(Skip = "Flaky")]
    public async Task BaseMessageConsumer_ShouldStartProcessing_WhenCancellationTokenIsExpiredLater()
    {
        var mockMessageSource = new Mock<IMessageSource>();
        var mockMessageDelegate = new Mock<IMessageDelegate>();
        var mockLogger = new Mock<ILogger>();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(millisecondsDelay: 100);

        var messageConsumer = new DerivedConsumer(mockMessageSource.Object, mockMessageDelegate.Object, mockLogger.Object);
        await messageConsumer.ExecuteAsync(cts.Token);

        mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task StartAsync_ShouldLogExceptions_WhenExceptionIsThrownWhileFetchingMessageFromSource()
    {
        var exception = new InvalidOperationException("Test Exception");

        var mockMessageSource = new Mock<IMessageSource>();
        mockMessageSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                         .ThrowsAsync(exception);

        var mockMessageDelegate = new Mock<IMessageDelegate>();
        var fakelogger = new FakeLogger();

        var messageConsumer = new SingleMessageConsumer(mockMessageSource.Object, mockMessageDelegate.Object, fakelogger);
        await messageConsumer.ExecuteAsync(CancellationToken.None);

        mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("MessageSource failed during reading message.", fakelogger.LatestRecord.Message, StringComparison.Ordinal);
        Assert.Equal(exception, fakelogger.LatestRecord.Exception);

        mockMessageDelegate.Verify(x => x.InvokeAsync(It.IsAny<MessageContext>()), Times.Never);
        mockMessageSource.Verify(x => x.Release(It.IsAny<MessageContext>()), Times.Never);
    }

    [Theory]
    [InlineData("message")]
    public async Task StartAsync_ShouldLogExceptions_WhenExceptionIsThrownDuringMessageProcessingCompletion(string message)
    {
        var mockFeatures = new Mock<IFeatureCollection>();
        MessageContext messageContext = CreateContext(mockFeatures.Object);
        messageContext.SetSourcePayload(Encoding.UTF8.GetBytes(message));

        var mockMessageSource = new Mock<IMessageSource>();
        mockMessageSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                         .Returns(new ValueTask<MessageContext>(messageContext));

        var mockMessageDelegate = new Mock<IMessageDelegate>();
        var fakelogger = new FakeLogger();

        var messageConsumer = new SingleMessageConsumer(mockMessageSource.Object, mockMessageDelegate.Object, fakelogger, true, false);
        await messageConsumer.ExecuteAsync(CancellationToken.None);

        mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockMessageDelegate.Verify(x => x.InvokeAsync(It.IsAny<MessageContext>()), Times.Once);

        Assert.Contains("Handling message procesing completion failed.", fakelogger.LatestRecord.Message, StringComparison.Ordinal);
        Assert.Equal(typeof(InvalidOperationException), fakelogger.LatestRecord.Exception?.GetType());

        mockMessageSource.Verify(x => x.Release(messageContext), Times.Once);
    }

    [Theory]
    [InlineData("message")]
    public async Task StartAsync_ShouldHandleExceptions_WhenExceptionIsThrownDuringProcessing(string message)
    {
        var exception = new InvalidOperationException("Error during processing.");
        var mockFeatures = new Mock<IFeatureCollection>();

        MessageContext messageContext = CreateContext(mockFeatures.Object);
        messageContext.SetSourcePayload(Encoding.UTF8.GetBytes(message));

        var mockMessageSource = new Mock<IMessageSource>();
        mockMessageSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                         .Returns(new ValueTask<MessageContext>(messageContext));

        var mockMessageDelegate = new Mock<IMessageDelegate>();
        mockMessageDelegate.Setup(x => x.InvokeAsync(It.IsAny<MessageContext>()))
                           .Throws(exception);

        var fakelogger = new FakeLogger();

        var messageConsumer = new SingleMessageConsumer(mockMessageSource.Object, mockMessageDelegate.Object, fakelogger);
        await messageConsumer.ExecuteAsync(CancellationToken.None);

        mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockMessageDelegate.Verify(x => x.InvokeAsync(It.IsAny<MessageContext>()), Times.Once);
        mockMessageSource.Verify(x => x.Release(messageContext), Times.Once);
    }

    [Theory]
    [InlineData("message")]
    public async Task StartAsync_ShouldLogExceptions_WhenExceptionIsThrownWhileHandlingFailureInProcessing(string message)
    {
        var processingException = new InvalidProgramException("Error during processing");
        var mockFeatures = new Mock<IFeatureCollection>();

        MessageContext messageContext = CreateContext(mockFeatures.Object);
        messageContext.SetSourcePayload(Encoding.UTF8.GetBytes(message));

        var mockMessageSource = new Mock<IMessageSource>();
        mockMessageSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                         .Returns(new ValueTask<MessageContext>(messageContext));

        var mockMessageDelegate = new Mock<IMessageDelegate>();
        mockMessageDelegate.Setup(x => x.InvokeAsync(It.IsAny<MessageContext>()))
                           .Throws(processingException);

        var fakelogger = new FakeLogger();

        var messageConsumer = new SingleMessageConsumer(mockMessageSource.Object, mockMessageDelegate.Object, fakelogger, false, true);
        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() => messageConsumer.ExecuteAsync(CancellationToken.None).AsTask());

        mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockMessageDelegate.Verify(x => x.InvokeAsync(It.IsAny<MessageContext>()), Times.Once);
        mockMessageSource.Verify(x => x.Release(messageContext), Times.Once);

        Assert.Contains("Handling message procesing failure failed with", fakelogger.LatestRecord.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidOperationException), fakelogger.LatestRecord.Message, StringComparison.Ordinal);
        Assert.Equal(processingException, fakelogger.LatestRecord.Exception);
    }

    [Theory]
    [InlineData("message")]
    public async Task StartAsync_ShouldLogExceptions_WhenMessageSourceThrowsExceptionDuringRelease(string message)
    {
        var exception = new InvalidOperationException("Error during releasing.");
        var mockFeatures = new Mock<IFeatureCollection>();

        MessageContext messageContext = CreateContext(mockFeatures.Object);
        messageContext.SetSourcePayload(Encoding.UTF8.GetBytes(message));

        var mockMessageSource = new Mock<IMessageSource>();
        mockMessageSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                         .Returns(new ValueTask<MessageContext>(messageContext));
        mockMessageSource.Setup(x => x.Release(It.IsAny<MessageContext>()))
                         .Throws(exception);

        var mockMessageDelegate = new Mock<IMessageDelegate>();
        var fakelogger = new FakeLogger();

        var messageConsumer = new SingleMessageConsumer(mockMessageSource.Object, mockMessageDelegate.Object, fakelogger);
        await messageConsumer.ExecuteAsync(CancellationToken.None);

        mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockMessageDelegate.Verify(x => x.InvokeAsync(It.IsAny<MessageContext>()), Times.Once);
        mockMessageSource.Verify(x => x.Release(messageContext), Times.Once);

        Assert.Contains("MessageSource failed during releasing context.", fakelogger.LatestRecord.Message, StringComparison.Ordinal);
        Assert.Equal(exception, fakelogger.LatestRecord.Exception);
    }
}

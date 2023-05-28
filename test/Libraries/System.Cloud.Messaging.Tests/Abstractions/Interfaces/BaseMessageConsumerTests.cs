// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.Tests.Data;
using System.Cloud.Messaging.Tests.Data.Consumers;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Moq;
using Xunit;

namespace System.Cloud.Messaging.Tests.Interfaces;

/// <summary>
/// Tests for <see cref="MessageConsumer"/>.
/// </summary>
public class BaseMessageConsumerTests
{
    private static MessageContext CreateContext(IFeatureCollection features, ReadOnlyMemory<byte> sourcePayload) => new TestMessageContext(features, sourcePayload);

    [Fact]
    public async Task BaseMessageConsumer_ShouldNotStartProcessing_WhenCancellationTokenIsExpired()
    {
        var mockMessageSource = new Mock<IMessageSource>();
        var mockMessageMiddlewares = new Mock<IReadOnlyList<IMessageMiddleware>>();
        var mockMessageDelegate = new Mock<MessageDelegate>();
        var mockLogger = new Mock<ILogger>();

        using var cts = new CancellationTokenSource();
        cts.Cancel(false);

        var messageConsumer = new DerivedConsumer(mockMessageSource.Object, mockMessageMiddlewares.Object, mockMessageDelegate.Object, mockLogger.Object);
        await messageConsumer.ExecuteAsync(cts.Token);

        mockMessageSource.VerifyNoOtherCalls();
        mockMessageDelegate.VerifyNoOtherCalls();
        mockLogger.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BaseMessageConsumer_ShouldStartProcessing_WhenCancellationTokenIsExpiredLater()
    {
        var mockMessageSource = new Mock<IMessageSource>();
        var mockMessageMiddlewares = new Mock<IReadOnlyList<IMessageMiddleware>>();
        var mockMessageDelegate = new Mock<MessageDelegate>();
        var mockLogger = new Mock<ILogger>();

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(millisecondsDelay: 100);

        var messageConsumer = new DerivedConsumer(mockMessageSource.Object, mockMessageMiddlewares.Object, mockMessageDelegate.Object, mockLogger.Object);
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

        var mockMessageMiddlewares = new Mock<IReadOnlyList<IMessageMiddleware>>();
        var mockMessageDelegate = new Mock<MessageDelegate>();
        var logger = new FakeLogger();

        var messageConsumer = new SingleMessageConsumer(mockMessageSource.Object, mockMessageMiddlewares.Object, mockMessageDelegate.Object, logger);
        await messageConsumer.ExecuteAsync(CancellationToken.None);

        mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains("MessageSource failed during reading message.", logger.LatestRecord.Message, StringComparison.Ordinal);
        Assert.Equal(exception, logger.LatestRecord.Exception);

        mockMessageDelegate.Verify(x => x.Invoke(It.IsAny<MessageContext>()), Times.Never);
        mockMessageSource.Verify(x => x.Release(It.IsAny<MessageContext>()), Times.Never);
    }

    [Theory]
    [InlineData("message")]
    public async Task StartAsync_ShouldLogExceptions_WhenExceptionIsThrownDuringMessageProcessingCompletion(string message)
    {
        var mockFeatures = new Mock<IFeatureCollection>();
        var messageContext = CreateContext(mockFeatures.Object, Encoding.UTF8.GetBytes(message));

        var mockMessageSource = new Mock<IMessageSource>();
        mockMessageSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                         .Returns(new ValueTask<MessageContext>(messageContext));

        var mockMessageMiddlewares = new Mock<IReadOnlyList<IMessageMiddleware>>();
        var mockMessageDelegate = new Mock<MessageDelegate>();
        var logger = new FakeLogger();

        var messageConsumer = new SingleMessageConsumer(mockMessageSource.Object, mockMessageMiddlewares.Object, mockMessageDelegate.Object, logger, true, false);
        await messageConsumer.ExecuteAsync(CancellationToken.None);

        mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockMessageDelegate.Verify(x => x.Invoke(It.IsAny<MessageContext>()), Times.Once);

        Assert.Contains("Completing message processing failed.", logger.LatestRecord.Message, StringComparison.Ordinal);
        Assert.Equal(typeof(InvalidOperationException), logger.LatestRecord.Exception?.GetType());

        mockMessageSource.Verify(x => x.Release(messageContext), Times.Once);
    }

    [Theory]
    [InlineData("message")]
    public async Task StartAsync_ShouldHandleExceptions_WhenExceptionIsThrownDuringProcessing(string message)
    {
        var exception = new InvalidOperationException("Error during processing.");
        var mockFeatures = new Mock<IFeatureCollection>();

        var messageContext = CreateContext(mockFeatures.Object, Encoding.UTF8.GetBytes(message));

        var mockMessageSource = new Mock<IMessageSource>();
        mockMessageSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                         .Returns(new ValueTask<MessageContext>(messageContext));

        var mockMessageMiddlewares = new Mock<IReadOnlyList<IMessageMiddleware>>();
        var mockMessageDelegate = new Mock<MessageDelegate>();
        mockMessageDelegate.Setup(x => x.Invoke(It.IsAny<MessageContext>()))
                           .Throws(exception);

        var logger = new FakeLogger();

        var messageConsumer = new SingleMessageConsumer(mockMessageSource.Object, mockMessageMiddlewares.Object, mockMessageDelegate.Object, logger);
        await messageConsumer.ExecuteAsync(CancellationToken.None);

        mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockMessageDelegate.Verify(x => x.Invoke(It.IsAny<MessageContext>()), Times.Once);
        mockMessageSource.Verify(x => x.Release(messageContext), Times.Once);
    }

    [Theory]
    [InlineData("message")]
    public async Task StartAsync_ShouldThrowExceptions_WhenExceptionIsThrownWhileHandlingFailureInProcessing(string message)
    {
        var processingException = new InvalidProgramException("Error during processing");
        var mockFeatures = new Mock<IFeatureCollection>();

        var messageContext = CreateContext(mockFeatures.Object, Encoding.UTF8.GetBytes(message));

        var mockMessageSource = new Mock<IMessageSource>();
        mockMessageSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                         .Returns(new ValueTask<MessageContext>(messageContext));

        var mockMessageMiddlewares = new Mock<IReadOnlyList<IMessageMiddleware>>();
        var mockMessageDelegate = new Mock<MessageDelegate>();
        mockMessageDelegate.Setup(x => x.Invoke(It.IsAny<MessageContext>()))
                           .Throws(processingException);

        var logger = new FakeLogger();

        var messageConsumer = new SingleMessageConsumer(mockMessageSource.Object, mockMessageMiddlewares.Object, mockMessageDelegate.Object, logger, false, true);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => messageConsumer.ExecuteAsync(CancellationToken.None).AsTask());

        mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockMessageDelegate.Verify(x => x.Invoke(It.IsAny<MessageContext>()), Times.Once);
        mockMessageSource.Verify(x => x.Release(messageContext), Times.Once);

        Assert.Contains("Handling message processing failure failed with", logger.LatestRecord.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(InvalidOperationException), logger.LatestRecord.Message, StringComparison.Ordinal);
        Assert.Equal(processingException, logger.LatestRecord.Exception);
    }

    [Theory]
    [InlineData("message")]
    public async Task StartAsync_ShouldLogExceptions_WhenMessageSourceThrowsExceptionDuringRelease(string message)
    {
        var exception = new InvalidOperationException("Error during releasing.");
        var mockFeatures = new Mock<IFeatureCollection>();

        var messageContext = CreateContext(mockFeatures.Object, Encoding.UTF8.GetBytes(message));

        var mockMessageSource = new Mock<IMessageSource>();
        mockMessageSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                         .Returns(new ValueTask<MessageContext>(messageContext));
        mockMessageSource.Setup(x => x.Release(It.IsAny<MessageContext>()))
                         .Throws(exception);

        var mockMessageMiddlewares = new Mock<IReadOnlyList<IMessageMiddleware>>();
        var mockMessageDelegate = new Mock<MessageDelegate>();
        var logger = new FakeLogger();

        var messageConsumer = new SingleMessageConsumer(mockMessageSource.Object, mockMessageMiddlewares.Object, mockMessageDelegate.Object, logger);
        await messageConsumer.ExecuteAsync(CancellationToken.None);

        mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockMessageDelegate.Verify(x => x.Invoke(It.IsAny<MessageContext>()), Times.Once);
        mockMessageSource.Verify(x => x.Release(messageContext), Times.Once);

        Assert.Contains("MessageSource failed while releasing context.", logger.LatestRecord.Message, StringComparison.Ordinal);
        Assert.Equal(exception, logger.LatestRecord.Exception);
    }
}

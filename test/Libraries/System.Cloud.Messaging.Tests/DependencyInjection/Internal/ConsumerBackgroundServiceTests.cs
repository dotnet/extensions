// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Cloud.Messaging.DependencyInjection.Internal;
using System.Cloud.Messaging.DependencyInjection.Tests.Data;
using System.Cloud.Messaging.DependencyInjection.Tests.Data.Consumers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace System.Cloud.Messaging.DependencyInjection.Tests.Internal;

/// <summary>
/// Tests for <see cref="ConsumerBackgroundService"/>.
/// </summary>
public class ConsumerBackgroundServiceTests
{
    [Fact]
    public async Task StopHostWithoutStart_ShouldNotCallConsumerStartMethod()
    {
        var mocks = new TestMocks().SetupMocks();
        var mockConsumer = mocks.Consumer();
        using var consumerBackgroundService = new ConsumerBackgroundService(mockConsumer);

        await consumerBackgroundService.StopAsync(CancellationToken.None);

        mocks.VerifyZeroInteractions();
    }

    [Fact]
    public async Task StopHostAfterStartingIt_ShouldCallConsumerMethods()
    {
        var mocks = new TestMocks().SetupMocks();
        var mockConsumer = mocks.Consumer();
        using var consumerBackgroundService = new ConsumerBackgroundService(mockConsumer);

        await consumerBackgroundService.StartAsync(CancellationToken.None);

        await Task.Delay(TimeSpan.FromSeconds(1));
        await consumerBackgroundService.StopAsync(CancellationToken.None);

        mocks.Verify(1);
    }

    private class TestMocks
    {
        private readonly Mock<IMessageSource> _mockMessageSource = new();
        private readonly Mock<IReadOnlyList<IMessageMiddleware>> _mockMessageMiddlewares = new();
        private readonly Mock<MessageDelegate> _mockDelegate = new();
        private readonly Mock<ILogger> _mockLogger = new();

        public TestMocks SetupMocks()
        {
            _mockMessageSource.Setup(x => x.ReadAsync(It.IsAny<CancellationToken>()))
                              .Returns(new ValueTask<MessageContext>(new TestMessageContext(new FeatureCollection(), ReadOnlyMemory<byte>.Empty)));
            return this;
        }

        public MessageConsumer Consumer() => new DerivedConsumer(_mockMessageSource.Object, _mockMessageMiddlewares.Object, _mockDelegate.Object, _mockLogger.Object);

        public void VerifyZeroInteractions()
        {
            _mockMessageSource.VerifyNoOtherCalls();
            _mockDelegate.VerifyNoOtherCalls();
            _mockLogger.VerifyNoOtherCalls();
        }

        public void Verify(int count)
        {
            _mockMessageSource.Verify(x => x.ReadAsync(It.IsAny<CancellationToken>()), Times.Exactly(count));
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;
using Polly.Timeout;
using Xunit;

namespace Microsoft.Extensions.Http
{
    public class PolicyHttpMessageHandlerTest
    {
        [Fact]
        public async Task SendAsync_PolicyTriggers_CanReexecuteSendAsync()
        {
            // Arrange
            var policy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .RetryAsync(retryCount: 5);

            var handler = new TestPolicyHttpMessageHandler(policy);

            var callCount = 0;
            var expected = new HttpResponseMessage();
            handler.InnerHandler = new MockHandler()
            {
                OnSendAsync = (req, ct) =>
                {
                    if (callCount == 0)
                    {
                        callCount++;
                        throw new HttpRequestException();
                    }
                    else if (callCount == 1)
                    {
                        callCount++;
                        return expected;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            };

            // Act
            var response = await handler.SendAsync(new HttpRequestMessage(), CancellationToken.None);

            // Assert
            Assert.Equal(2, callCount);
            Assert.Same(expected, response);
        }

        [Fact]
        public async Task SendAsync_PolicyCancellation_CanTriggerRequestCancellation()
        {
            // Arrange
            var policy = Policy<HttpResponseMessage>
                .Handle<TimeoutRejectedException>() // Handle timeouts by retrying
                .RetryAsync(retryCount: 5)
                .WrapAsync(Policy
                    .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromMilliseconds(50)) // Apply a 50ms timeout
                    .WrapAsync(Policy.NoOpAsync<HttpResponseMessage>()));

            var handler = new TestPolicyHttpMessageHandler(policy);

            var @event = new ManualResetEventSlim(initialState: false);

            var callCount = 0;
            var expected = new HttpResponseMessage();
            handler.InnerHandler = new MockHandler()
            {
                // The inner cancellation token is created by polly, it will trigger the timeout.
                OnSendAsync = (req, ct) =>
                {
                    Assert.True(ct.CanBeCanceled);
                    if (callCount == 0)
                    {
                        callCount++;
                        @event.Wait(ct);
                        throw null; // unreachable, previous line should throw
                    }
                    else if (callCount == 1)
                    {
                        callCount++;
                        return expected;
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            };

            // Act
            var response = await handler.SendAsync(new HttpRequestMessage(), CancellationToken.None);

            // Assert
            Assert.Equal(2, callCount);
            Assert.Same(expected, response);
        }

        private class TestPolicyHttpMessageHandler : PolicyHttpMessageHandler
        {
            public TestPolicyHttpMessageHandler(IAsyncPolicy<HttpResponseMessage> policy)
                : base(policy)
            {
            }

            public new Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return base.SendAsync(request, cancellationToken);
            }
        }

        private class MockHandler : DelegatingHandler
        {
            public Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> OnSendAsync { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(OnSendAsync(request, cancellationToken));
            }
        }
    }
}

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
            handler.OnSendAsync = (req, c, ct) =>
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
            handler.OnSendAsync = (req, c, ct) =>
            {
                // The inner cancellation token is created by polly, it will trigger the timeout.
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
            };

            // Act
            var response = await handler.SendAsync(new HttpRequestMessage(), CancellationToken.None);

            // Assert
            Assert.Equal(2, callCount);
            Assert.Same(expected, response);
        }

        [Fact]
        public async Task SendAsync_NoContextSet_CreatesNewContext()
        {
            // Arrange
            var policy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
            var handler = new TestPolicyHttpMessageHandler(policy);

            Context context = null;
            var expected = new HttpResponseMessage();
            handler.OnSendAsync = (req, c, ct) =>
            {
                context = c;
                Assert.NotNull(context);
                Assert.Same(context, req.GetPolicyExecutionContext());
                return expected;
            };

            var request = new HttpRequestMessage();

            // Act
            var response = await handler.SendAsync(request, CancellationToken.None);

            // Assert
            Assert.Same(context, request.GetPolicyExecutionContext()); // We don't clean up the context
            Assert.Same(expected, response);
        }

        [Fact]
        public async Task SendAsync_ExistingContext_ReusesContext()
        {
            // Arrange
            var policy = Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10));
            var handler = new TestPolicyHttpMessageHandler(policy);

            var expected = new HttpResponseMessage();
            var expectedContext = new Context(Guid.NewGuid().ToString());

            Context context = null;
            handler.OnSendAsync = (req, c, ct) =>
            {
                context = c;
                Assert.NotNull(c);
                Assert.Same(c, req.GetPolicyExecutionContext());
                return expected;
            };

            var request = new HttpRequestMessage();
            request.SetPolicyExecutionContext(expectedContext);

            // Act
            var response = await handler.SendAsync(request, CancellationToken.None);

            // Assert
            Assert.Same(expectedContext, context);
            Assert.Same(expectedContext, request.GetPolicyExecutionContext()); // We don't clean up the context
            Assert.Same(expected, response);
        }

        private class TestPolicyHttpMessageHandler : PolicyHttpMessageHandler
        {
            public Func<HttpRequestMessage, Context, CancellationToken, HttpResponseMessage> OnSendAsync { get; set; }

            public TestPolicyHttpMessageHandler(IAsyncPolicy<HttpResponseMessage> policy)
                : base(policy)
            {
            }

            public new Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return base.SendAsync(request, cancellationToken);
            }

            protected override Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, Context context, CancellationToken cancellationToken)
            {
                Assert.NotNull(OnSendAsync);
                return Task.FromResult(OnSendAsync(request, context, cancellationToken));
            }
        }
    }
}

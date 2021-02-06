// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServerClient.Razor.HtmlCSharp
{
    public class DefaultLSPProgressListenerTest
    {
        // Long timeout after last notification to avoid triggering even in slow CI environments
        private static readonly TimeSpan NotificationTimeout = TimeSpan.FromSeconds(20);

        [Fact]
        public void TryListenForProgress_ReturnsTrue()
        {
            // Arrange
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);

            var token = Guid.NewGuid().ToString();
            using var cts = new CancellationTokenSource();

            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            // Act
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: async (value, ct) => { await Task.Delay(1).ConfigureAwait(false); },
                delayAfterLastNotifyAsync: cancellationToken => Task.Delay(NotificationTimeout, cancellationToken),
                cts.Token,
                out var onCompleted);

            // Assert
            Assert.True(listenerAdded);
            Assert.NotNull(onCompleted);
            Assert.False(onCompleted.IsCompleted);
        }

        [Fact]
        public void TryListenForProgress_DuplicateRegistration_ReturnsFalse()
        {
            // Arrange
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);

            var token = Guid.NewGuid().ToString();
            using var cts = new CancellationTokenSource();

            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            // Act
            _ = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: async (value, ct) => { await Task.Delay(1).ConfigureAwait(false); },
                delayAfterLastNotifyAsync: cancellationToken => Task.Delay(NotificationTimeout, cancellationToken),
                cts.Token,
                out _);
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: async (value, ct) => { await Task.Delay(1).ConfigureAwait(false); },
                delayAfterLastNotifyAsync: cancellationToken => Task.Delay(NotificationTimeout, cancellationToken),
                cts.Token,
                out var onCompleted);

            // Assert
            Assert.False(listenerAdded);
            Assert.Null(onCompleted);
        }

        [Fact]
        public async Task TryListenForProgress_TaskNotificationTimeoutAfterNoInitialProgress()
        {
            // Arrange
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);

            var token = Guid.NewGuid().ToString();
            var notificationTimeout = TimeSpan.FromSeconds(1);
            using var cts = new CancellationTokenSource();
            var onProgressNotifyAsyncCalled = false;

            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            // Act 1
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: (value, ct) => {
                    onProgressNotifyAsyncCalled = true;
                    return Task.CompletedTask;
                },
                delayAfterLastNotifyAsync: cancellationToken => Task.Delay(notificationTimeout, cancellationToken),
                cts.Token,
                out var onCompleted);

            // Assert 1
            Assert.True(listenerAdded);
            Assert.NotNull(onCompleted);
            Assert.False(onCompleted.IsCompleted, "Task completed immediately, should wait for timeout");

            // Act 2
            await onCompleted.ConfigureAwait(false);

            // Assert 2
            Assert.True(onCompleted.IsCompleted);
            Assert.False(onProgressNotifyAsyncCalled);
        }

        [Fact]
        public async Task TryListenForProgress_ProgressNotificationInvalid()
        {
            // Arrange
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);

            var token = Guid.NewGuid().ToString();
            using var cts = new CancellationTokenSource();
            var onProgressNotifyAsyncCalled = false;

            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            // Act
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: (value, ct) => {
                    onProgressNotifyAsyncCalled = true;
                    return Task.CompletedTask;
                },
                delayAfterLastNotifyAsync: cancellationToken => Task.Delay(TimeSpan.FromSeconds(1), cancellationToken),
                cts.Token,
                out var onCompleted);

            // Note `Methods.ClientRegisterCapabilityName` is the wrong method, instead of `Methods.ProgressNotificationName`
            await lspProgressListener.ProcessProgressNotificationAsync(Methods.ClientRegisterCapabilityName, new JObject()).ConfigureAwait(false);
            await onCompleted.ConfigureAwait(false);

            // Assert
            Assert.False(onProgressNotifyAsyncCalled);
        }

        [Fact]
        public async Task TryListenForProgress_SingleProgressNotificationReported()
        {
            // Arrange
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);

            var token = Guid.NewGuid().ToString();
            using var cts = new CancellationTokenSource();

            var expectedValue = "abcxyz";
            var parameterToken = new JObject
            {
                { Methods.ProgressNotificationTokenName, token },
                { "value", JArray.FromObject(new[] { expectedValue }) }
            };

            using var completedTokenSource = new CancellationTokenSource();
            var onProgressNotifyAsyncCalled = false;
            Task onProgressNotifyAsync(JToken value, CancellationToken ct)
            {
                var result = value.ToObject<string[]>();
                var firstValue = Assert.Single(result);
                Assert.Equal(expectedValue, firstValue);
                onProgressNotifyAsyncCalled = true;
                completedTokenSource.CancelAfter(0);
                return Task.CompletedTask;
            }

            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            // Act
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: onProgressNotifyAsync,
                delayAfterLastNotifyAsync: cancellationToken => DelayAfterLastNotifyAsync(NotificationTimeout, completedTokenSource.Token, cancellationToken),
                cts.Token,
                out var onCompleted);

            await lspProgressListener.ProcessProgressNotificationAsync(Methods.ProgressNotificationName, parameterToken).ConfigureAwait(false);
            await onCompleted.ConfigureAwait(false);

            // Assert
            Assert.True(onProgressNotifyAsyncCalled);
        }

        [Fact]
        public async Task TryListenForProgress_MultipleProgressNotificationReported()
        {
            // Arrange
            const int NUM_NOTIFICATIONS = 50;
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>(MockBehavior.Strict);

            var token = Guid.NewGuid().ToString();
            using var cts = new CancellationTokenSource();

            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            var parameterTokens = new List<JObject>();
            for (var i = 0; i < NUM_NOTIFICATIONS; ++i)
            {
                parameterTokens.Add(new JObject
                {
                    { Methods.ProgressNotificationTokenName, token },
                    { "value", i }
                });
            }

            using var completedTokenSource = new CancellationTokenSource();
            var receivedResults = new ConcurrentBag<int>();
            Task onProgressNotifyAsync(JToken value, CancellationToken ct)
            {
                receivedResults.Add(value.ToObject<int>());
                if (receivedResults.Count == NUM_NOTIFICATIONS)
                {
                    // All notifications received
                    completedTokenSource.CancelAfter(0);
                }

                return Task.CompletedTask;
            }

            // Act
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: onProgressNotifyAsync,
                delayAfterLastNotifyAsync: cancellationToken => DelayAfterLastNotifyAsync(NotificationTimeout, completedTokenSource.Token, cancellationToken),
                cts.Token,
                out var onCompleted);

            Parallel.ForEach(parameterTokens, parameterToken =>
            {
                _ = lspProgressListener.ProcessProgressNotificationAsync(Methods.ProgressNotificationName, parameterToken);
            });
            await onCompleted.ConfigureAwait(false);

            // Assert
            Assert.True(listenerAdded);
            var sortedResults = receivedResults.ToList();
            sortedResults.Sort();
            for (var i = 0; i < NUM_NOTIFICATIONS; ++i)
            {
                Assert.Equal(i, sortedResults[i]);
            }
        }

        private static async Task DelayAfterLastNotifyAsync(TimeSpan waitForProgressNotificationTimeout, CancellationToken immediateNotificationTimeout, CancellationToken cancellationToken)
        {
            using var combined = immediateNotificationTimeout.CombineWith(cancellationToken);

            try
            {
                await Task.Delay(waitForProgressNotificationTimeout, combined.Token).ConfigureAwait(false);
            }
            catch (TaskCanceledException) when (immediateNotificationTimeout.IsCancellationRequested)
            {
                // The delay was requested to complete immediately
            }
        }
    }
}

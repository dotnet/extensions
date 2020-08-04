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
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>();

            var token = Guid.NewGuid().ToString();
            using var cts = new CancellationTokenSource();

            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            // Act
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: async (value, ct) => { await Task.Delay(1); },
                NotificationTimeout,
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
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>();

            var token = Guid.NewGuid().ToString();
            using var cts = new CancellationTokenSource();

            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            // Act
            _ = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: async (value, ct) => { await Task.Delay(1); },
                NotificationTimeout,
                cts.Token,
                out _);
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: async (value, ct) => { await Task.Delay(1); },
                NotificationTimeout,
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
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>();

            var token = Guid.NewGuid().ToString();
            var notificationTimeout = TimeSpan.FromSeconds(15);
            using var cts = new CancellationTokenSource();
            var onProgressNotifyAsyncCalled = false;

            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            // Act 1
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: async (value, ct) => {
                    await Task.Delay(1);
                    onProgressNotifyAsyncCalled = true;
                },
                notificationTimeout,
                cts.Token,
                out var onCompleted);

            // Assert 1
            Assert.True(listenerAdded);
            Assert.NotNull(onCompleted);
            Assert.False(onCompleted.IsCompleted, "Task completed immediately, should wait for timeout");

            // Act 2
            await onCompleted;

            // Assert 2
            Assert.True(onCompleted.IsCompleted);
            Assert.False(onProgressNotifyAsyncCalled);
        }

        [Fact]
        public async Task TryListenForProgress_ProgressNotificationInvalid()
        {
            // Arrange
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>();

            var token = Guid.NewGuid().ToString();
            using var cts = new CancellationTokenSource();
            var onProgressNotifyAsyncCalled = false;

            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            // Act
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: async (value, ct) => {
                    await Task.Delay(1);
                    onProgressNotifyAsyncCalled = true;
                },
                NotificationTimeout,
                cts.Token,
                out var onCompleted);

            // Note `Methods.ClientRegisterCapabilityName` is the wrong method, instead of `Methods.ProgressNotificationName`
            await lspProgressListener.ProcessProgressNotificationAsync(Methods.ClientRegisterCapabilityName, new JObject());
            await onCompleted;

            // Assert
            Assert.False(onProgressNotifyAsyncCalled);
        }

        [Fact]
        public async Task TryListenForProgress_SingleProgressNotificationReported()
        {
            // Arrange
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>();

            var token = Guid.NewGuid().ToString();
            using var cts = new CancellationTokenSource();

            var expectedValue = "abcxyz";
            var parameterToken = new JObject
            {
                { Methods.ProgressNotificationTokenName, token },
                { "value", JArray.FromObject(new[] { expectedValue }) }
            };

            var onProgressNotifyAsyncCalled = false;
            Func<JToken, CancellationToken, Task> onProgressNotifyAsync = (value, ct) => {
                var result = value.ToObject<string[]>();
                var firstValue = Assert.Single(result);
                Assert.Equal(expectedValue, firstValue);
                onProgressNotifyAsyncCalled = true;
                return Task.CompletedTask;
            };

            using var lspProgressListener = new DefaultLSPProgressListener(languageServiceBroker);

            // Act
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: onProgressNotifyAsync,
                NotificationTimeout,
                cts.Token,
                out var onCompleted);

            await lspProgressListener.ProcessProgressNotificationAsync(Methods.ProgressNotificationName, parameterToken);
            await onCompleted;

            // Assert
            Assert.True(onProgressNotifyAsyncCalled);
        }

        [Fact]
        public async Task TryListenForProgress_MultipleProgressNotificationReported()
        {
            // Arrange
            const int NUM_NOTIFICATIONS = 50;
            var languageServiceBroker = Mock.Of<ILanguageServiceBroker2>();

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

            var receivedResults = new ConcurrentBag<int>();
            Func<JToken, CancellationToken, Task> onProgressNotifyAsync = (value, ct) => {
                receivedResults.Add(value.ToObject<int>());
                return Task.CompletedTask;
            };

            // Act
            var listenerAdded = lspProgressListener.TryListenForProgress(
                token,
                onProgressNotifyAsync: onProgressNotifyAsync,
                NotificationTimeout,
                cts.Token,
                out var onCompleted);

            Parallel.ForEach(parameterTokens, parameterToken =>
            {
                _ = lspProgressListener.ProcessProgressNotificationAsync(Methods.ProgressNotificationName, parameterToken);
            });
            await onCompleted;

            // Assert
            Assert.True(listenerAdded);
            var sortedResults = receivedResults.ToList();
            sortedResults.Sort();
            for (var i = 0; i < NUM_NOTIFICATIONS; ++i)
            {
                Assert.Equal(i, sortedResults[i]);
            }
        }
    }
}

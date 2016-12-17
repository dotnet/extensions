// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Moq;
using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Primitives.Tests
{
    public class CompositeChangeTokenTest
    {
        [Fact]
        public void RegisterChangeCallback_IsInvokedExactlyOnce()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();
            var cancellationToken = cancellationTokenSource.Token;
            var firstCancellationChangeToken = new CancellationChangeToken(cancellationToken);
            var secondCancellationChangeToken = new CancellationChangeToken(cancellationToken);
            var thirdCancellationChangeToken = new CancellationChangeToken(cancellationToken);

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstCancellationChangeToken, secondCancellationChangeToken, thirdCancellationChangeToken });
            //var firstChangeToken = new MockChangeToken();
            //var secondChangeToken = new MockChangeToken() { ActiveChangeCallbacks = true };
            //var thirdChangeToken = new MockChangeToken();
            //var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken, secondChangeToken, thirdChangeToken });
            var count1 = 0;
            var count2 = 0;
            compositeChangeToken.RegisterChangeCallback(_ => count1++, null);
            compositeChangeToken.RegisterChangeCallback(_ => count2++, null);

            // Act
            cancellationTokenSource.Cancel();

            // Assert
            Assert.Equal(1, count1);
            Assert.Equal(1, count2);
        }

        [Fact]
        public void HasChanged_IsTrue_IfAnyTokenHasChanged()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken { HasChanged = true };
            var thirdChangeToken = new MockChangeToken();

            // Act
            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken, secondChangeToken, thirdChangeToken });
            compositeChangeToken.RegisterChangeCallback(item => new object(), null);

            // Assert
            Assert.True(compositeChangeToken.HasChanged);
        }

        [Fact]
        public void HasChanged_IsFalse_IfNoTokenHasChanged()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken();

            // Act
            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken, secondChangeToken });
            compositeChangeToken.RegisterChangeCallback(item => new object(), null);

            // Assert
            Assert.False(compositeChangeToken.HasChanged);
        }

        [Fact]
        public void ActiveChangeCallbacks_IsTrue_IfAnyTokenHasActiveChangeCallbacks()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken() { ActiveChangeCallbacks = true };
            var thirdChangeToken = new MockChangeToken();

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken, secondChangeToken, thirdChangeToken });

            // Act & Assert
            Assert.True(compositeChangeToken.ActiveChangeCallbacks);
        }

        [Fact]
        public void ActiveChangeCallbacks_IsFalse_IfNoTokenHasActiveChangeCallbacks()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken();

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken, secondChangeToken });

            // Act & Assert
            Assert.False(compositeChangeToken.ActiveChangeCallbacks);
        }

        [Fact]
        public async Task CallbackRaisedOnce_ConcurrentThreadsBlocked()
        {
            // Arrange
            var event1 = new ManualResetEvent(false);
            var event2 = new ManualResetEvent(false);
            var event3 = new ManualResetEvent(false);

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;
            var cancellationChangeToken = new CancellationChangeToken(cancellationToken);
            var count = 0;
            Action<object> callback = _ =>
            {
                count++;
                event3.Set();
                event1.WaitOne(5000);
            };

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { cancellationChangeToken });
            compositeChangeToken.RegisterChangeCallback(callback, null);

            // Act
            var firstChange = Task.Run(() =>
            {
                event2.WaitOne(5000);
                compositeChangeToken._cancellationTokenSource.Cancel();
            });
            var secondChange = Task.Run(() =>
            {
                event3.WaitOne(5000);
                compositeChangeToken._cancellationTokenSource.Cancel();
                event1.Set();
            });

            event2.Set();

            await Task.WhenAll(firstChange, secondChange);

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task RegisterChangeCallbackOnce_ConcurrentThreadsBlocked()
        {
            // Arrange
            var event1 = new ManualResetEvent(false);
            var event2 = new ManualResetEvent(false);
            var event3 = new ManualResetEvent(false);

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationChangeToken = new CancellationChangeToken(cancellationTokenSource.Token);

            var count = 0;
            Action<object> callback = _ =>
            {
                count++;
                event3.Set();
                event1.WaitOne();
            };

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { cancellationChangeToken });
            compositeChangeToken.RegisterChangeCallback(callback, null);

            // Act
            var firstChange = Task.Run(() =>
            {
                event2.WaitOne(5000);
                cancellationTokenSource.Cancel();
            });

            var secondChange = Task.Run(() =>
            {
                event3.WaitOne();
                compositeChangeToken.RegisterChangeCallback(callback, null);
                event1.Set();
            });

            event2.Set();

            await Task.WhenAll(firstChange, secondChange);

            // Assert
            Assert.Equal(1, count);
        }
    }
}

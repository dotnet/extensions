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
            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken();
            var thirdChangeToken = new MockChangeToken();
            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken, secondChangeToken, thirdChangeToken });
            var count1 = 0;
            var count2 = 0;

            compositeChangeToken.RegisterChangeCallback(_ => count1++, null);
            compositeChangeToken.RegisterChangeCallback(_ => count2++, null);

            // Act
            firstChangeToken.RaiseCallback(null);
            secondChangeToken.RaiseCallback(null);
            thirdChangeToken.RaiseCallback(null);

            // Assert
            Assert.Equal(1, count1);
            Assert.Equal(1, count2);
        }

        [Fact]
        public void RegisterChangeCallback_ReturnsACompositeDisposable()
        {
            // Arrange
            var firstChangeToken = new Mock<IChangeToken>();
            var secondChangeToken = new Mock<IChangeToken>();

            object result = null;
            object state = new object();
            Action<object> callback = item => result = item;

            var compositeDisposable = new MockDisposable();

            var compositeChangeToken = new Mock<CompositeChangeToken>(new List<IChangeToken> { firstChangeToken.Object, secondChangeToken.Object });
            compositeChangeToken.Setup(t => t.RegisterChangeCallback(callback, state))
              .Returns(() =>
              {
                  var disposable = new MockDisposable();
                  disposable.Dispose();
                  compositeDisposable = disposable;
                  return disposable;
              });

            // Act
            compositeChangeToken.Object.RegisterChangeCallback(callback, state);
            compositeDisposable.Dispose();

            // Assert
            Assert.True(compositeDisposable.Disposed);
        }

        [Fact]
        public void HasChanged_IsTrue_IfAnyTokenHasChanged()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken { HasChanged = true };
            var thirdChangeToken = new MockChangeToken();

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken, secondChangeToken, thirdChangeToken });

            // Act & Assert
            Assert.True(compositeChangeToken.HasChanged);
        }

        [Fact]
        public void HasChanged_IsFalse_IfNoTokenHasChanged()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken();

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken, secondChangeToken });

            // Act & Assert
            Assert.False(compositeChangeToken.HasChanged);
        }

        [Fact]
        public void ActiveChangeCallbacks_IsTrue_IfAnyTokenHasActiveChangeCallbacks()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken { ActiveChangeCallbacks = true };
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
        public async Task CallbackInvokedOnce_ConcurrentThreadsBlocked()
        {
            // Arrange
            var event1 = new AutoResetEvent(false);
            var event2 = new AutoResetEvent(false);
            var event3 = new AutoResetEvent(false);

            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken();
            var count = 0;
            Action<object> callback = _ =>
            {
                count++;
                event3.Set();
                event1.WaitOne(5000);
            };

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken, secondChangeToken });
            compositeChangeToken.RegisterChangeCallback(callback, null);

            // Act
            var firstChange = Task.Run(() =>
            {
                event2.WaitOne(5000);
                firstChangeToken.RaiseCallback(null);
            });
            var secondChange = Task.Run(() =>
            {
                event3.WaitOne(5000);
                secondChangeToken.RaiseCallback(null);
            }).ContinueWith(x => event1.Set());

            event2.Set();

            await Task.WhenAll(firstChange, secondChange);

            // Assert
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task RegisterChangeCallbackOnce_ConcurrentThreadsBlocked()
        {
            // Arrange
            var event1 = new AutoResetEvent(false);
            var event2 = new AutoResetEvent(false);
            var event3 = new AutoResetEvent(false);

            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken();
            var count = 0;
            Action<object> callback = _ =>
            {
                count++;
                event3.Set();
                event1.WaitOne();
            };

            var compositeChangeToken = new CompositeChangeToken(new List<IChangeToken> { firstChangeToken, secondChangeToken });
            compositeChangeToken.RegisterChangeCallback(callback, null);

            // Act
            var firstChange = Task.Run(() =>
            {
                event2.WaitOne(5000);
                firstChangeToken.RaiseCallback(null);
            });
            var secondChange = Task.Run(() =>
            {
                event3.WaitOne();
                compositeChangeToken.RegisterChangeCallback(callback, null);
            }).ContinueWith(x => event1.Set());

            event2.Set();

            await Task.WhenAll(firstChange, secondChange);

            // Assert
            Assert.Equal(1, count);
        }
    }
}

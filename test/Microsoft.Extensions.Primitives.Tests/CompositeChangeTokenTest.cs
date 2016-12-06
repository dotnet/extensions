// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Primitives.Tests
{
    public class CompositeChangeTokenTest
    {
        [Fact]
        public void RegisterChangeCallback_ReturnsACompositeDisposable()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken();

            var changeTokens = new List<IChangeToken>();
            changeTokens.Add(firstChangeToken);
            changeTokens.Add(secondChangeToken);

            object result = null;
            object state = new object();

            // Act
            var compositeChangeToken = new CompositeChangeToken(changeTokens);
            var disposable = compositeChangeToken.RegisterChangeCallback(item =>
            {
                result = item;
            }, state);

            // Assert
            Assert.Equal(1, firstChangeToken.Callbacks.Count);
            Assert.False(firstChangeToken.Callbacks[0].Item3.Disposed);
            Assert.Equal(1, secondChangeToken.Callbacks.Count);
            Assert.False(secondChangeToken.Callbacks[0].Item3.Disposed);

            disposable.Dispose();
            Assert.Equal(1, firstChangeToken.Callbacks.Count);
            Assert.True(firstChangeToken.Callbacks[0].Item3.Disposed);
            Assert.Equal(1, secondChangeToken.Callbacks.Count);
            Assert.True(secondChangeToken.Callbacks[0].Item3.Disposed);
        }

        [Fact]
        public void HasChanged_IsTrue_IfAnyTokenHasChanged()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken { HasChanged = true };
            var secondChangeToken = new MockChangeToken();

            var changeTokens = new List<IChangeToken>();
            changeTokens.Add(firstChangeToken);
            changeTokens.Add(secondChangeToken);

            // Act
            var compositeChangeToken = new CompositeChangeToken(changeTokens);

            // Assert
            Assert.True(compositeChangeToken.HasChanged);
        }

        [Fact]
        public void HasChanged_IsFalse_IfNoTokenHasChanged()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken();

            var changeTokens = new List<IChangeToken>();
            changeTokens.Add(firstChangeToken);
            changeTokens.Add(secondChangeToken);

            // Act
            var compositeChangeToken = new CompositeChangeToken(changeTokens);

            // Assert
            Assert.False(compositeChangeToken.HasChanged);
        }

        [Fact]
        public void ActiveChangeCallbacks_IsTrue_IfAnyTokenHasActiveChangeCallbacks()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken { ActiveChangeCallbacks = true };
            var secondChangeToken = new MockChangeToken();

            var changeTokens = new List<IChangeToken>();
            changeTokens.Add(firstChangeToken);
            changeTokens.Add(secondChangeToken);

            // Act
            var compositeChangeToken = new CompositeChangeToken(changeTokens);

            // Assert
            Assert.True(compositeChangeToken.ActiveChangeCallbacks);
        }

        [Fact]
        public void ActiveChangeCallbacks_IsFalse_IfNoTokenHasActiveChangeCallbacks()
        {
            // Arrange
            var firstChangeToken = new MockChangeToken();
            var secondChangeToken = new MockChangeToken();

            var changeTokens = new List<IChangeToken>();
            changeTokens.Add(firstChangeToken);
            changeTokens.Add(secondChangeToken);

            // Act
            var compositeChangeToken = new CompositeChangeToken(changeTokens);

            // Assert
            Assert.False(compositeChangeToken.ActiveChangeCallbacks);
        }
    }
}

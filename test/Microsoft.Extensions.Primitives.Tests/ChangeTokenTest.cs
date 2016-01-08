// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.Extensions.Primitives
{
    public class ChangeTokenTests
    {
        public class TestChangeToken : IChangeToken
        {
            public bool ActiveChangeCallbacks { get; set; }
            public bool HasChanged { get; set; }

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                _callback = () => callback(state);
                return null;
            }

            public void Changed()
            {
                HasChanged = true;
                _callback();
            }

            private Action _callback;
        }

        [Fact]
        public void HasChangeFiresChange()
        {
            var token = new TestChangeToken();
            bool fired = false;
            ChangeToken.OnChange(() => token, () => fired = true);
            Assert.False(fired);
            token.Changed();
            Assert.True(fired);
        }

        [Fact]
        public void HasChangeFiresChangeWithState()
        {
            var token = new TestChangeToken();
            object state = new object();
            object callbackState = null;
            ChangeToken.OnChange(() => token, s => callbackState = s, state);
            Assert.Null(callbackState);
            token.Changed();
            Assert.Equal(state, callbackState);
        }
    }
}

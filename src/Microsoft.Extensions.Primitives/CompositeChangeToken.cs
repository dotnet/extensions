// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// An <see cref="IChangeToken" /> which represents one or more <see cref="IChangeToken" /> instances.
    /// </summary>
    public class CompositeChangeToken : IChangeToken
    {
        private readonly object _callbackLock = new object();
        private List<CallbackState> _callbacks = new List<CallbackState>();
        private bool _registeredCallbackProxy;
        private bool _canBeChanged = true;

        /// <summary>
        /// Creates a new instance of <see cref="CompositeChangeToken"/>.
        /// </summary>
        /// <param name="changeTokens">The list of <see cref="IChangeToken"/> to compose.</param>
        public CompositeChangeToken(IReadOnlyList<IChangeToken> changeTokens)
        {
            if (changeTokens == null)
            {
                throw new ArgumentNullException(nameof(changeTokens));
            }

            ChangeTokens = changeTokens;
        }

        /// <summary>
        /// Returns the list of <see cref="IChangeToken"/> which compose the current <see cref="CompositeChangeToken"/>.
        /// </summary>
        public IReadOnlyList<IChangeToken> ChangeTokens { get; }

        /// <inheritdoc />
        public virtual IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            if (!_canBeChanged)
            {
                return NullDisposable.Singleton;
            }

            var changeTokenState = new CallbackState(callback, state, new CallbackDisposable());
            _callbacks.Add(changeTokenState);

            EnsureCallbacksRegistered();

            return changeTokenState.Disposable;
        }

        private void EnsureCallbacksRegistered()
        {
            lock (_callbackLock)
            {
                if (_registeredCallbackProxy)
                {
                    return;
                }

                for (var i = 0; i < ChangeTokens.Count; i++)
                {
                    ChangeTokens[i].RegisterChangeCallback(OnChange, null);
                }

                _registeredCallbackProxy = true;
            }
        }

        private void OnChange(object state)
        {
            if (!_canBeChanged)
            {
                return;
            }

            _canBeChanged = false;
            lock (_callbackLock)
            {
                for (var i = 0; i < _callbacks.Count; i++)
                {
                    var callbackState = _callbacks[i];
                    if (callbackState.Disposable.Disposed)
                    {
                        continue;
                    }

                    callbackState.Callback(callbackState.State);
                }
            }
        }

        private class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Singleton = new NullDisposable();

            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private class CallbackDisposable : IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private struct CallbackState
        {
            public CallbackState(Action<object> callback, object state, CallbackDisposable disposable)
            {
                Callback = callback;
                State = state;
                Disposable = disposable;
            }

            public Action<object> Callback { get; }
            public object State { get; }
            public CallbackDisposable Disposable { get; }
        }

        /// <inheritdoc />
        public bool HasChanged
        {
            get
            {
                for (var i = 0; i < ChangeTokens.Count; i++)
                {
                    if (ChangeTokens[i].HasChanged)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <inheritdoc />
        public bool ActiveChangeCallbacks
        {
            get
            {
                for (var i = 0; i < ChangeTokens.Count; i++)
                {
                    if (ChangeTokens[i].ActiveChangeCallbacks)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

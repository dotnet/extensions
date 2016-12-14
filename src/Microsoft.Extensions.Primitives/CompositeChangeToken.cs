// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// An <see cref="IChangeToken"/> which represents one or more <see cref="IChangeToken"/> instances.
    /// </summary>
    public class CompositeChangeToken : IChangeToken
    {
        private static readonly Action<object> _onChangeDelegate = OnChange;
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
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

            EnsureCallbacksRegistered();
            return _cancellationTokenSource.Token.Register(callback, state);
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

        private void EnsureCallbacksRegistered()
        {
            if (_registeredCallbackProxy)
            {
                return;
            }

            for (var i = 0; i < ChangeTokens.Count; i++)
            {
                if (ActiveChangeCallbacks)
                {
                    ChangeTokens[i].RegisterChangeCallback(_onChangeDelegate, this);
                }
            }
            _registeredCallbackProxy = true;
        }

        private static void OnChange(object state)
        {
            var compositeChangeTokenState = (CompositeChangeToken)state;
            if (!compositeChangeTokenState._canBeChanged)
            {
                return;
            }

            compositeChangeTokenState._canBeChanged = false;
            compositeChangeTokenState._cancellationTokenSource.Cancel();
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
    }
}

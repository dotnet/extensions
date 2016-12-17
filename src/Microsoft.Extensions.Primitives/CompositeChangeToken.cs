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
        public CancellationTokenSource _cancellationTokenSource;
        private readonly object _callbackLock = new object();
        private bool _registeredCallbackProxy;
        private List<IDisposable> _disposables;

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

            ActiveChangeCallbacks = false;
            for (var i = 0; i < ChangeTokens.Count; i++)
            {
                if (ChangeTokens[i].ActiveChangeCallbacks)
                {
                    ActiveChangeCallbacks = true;
                }
            }
        }

        /// <summary>
        /// Returns the list of <see cref="IChangeToken"/> which compose the current <see cref="CompositeChangeToken"/>.
        /// </summary>
        public IReadOnlyList<IChangeToken> ChangeTokens { get; }

        /// <inheritdoc />
        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            EnsureCallbacksInitialized();
            if (!_cancellationTokenSource.IsCancellationRequested)
            {
                return _cancellationTokenSource.Token.Register(callback, state);
            }

            else
            {
                return null;
            }
        }

        /// <inheritdoc />
        public bool HasChanged
        {
            get
            {
                if (_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    return true;
                }

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
        public bool ActiveChangeCallbacks { get; }

        private void EnsureCallbacksInitialized()
        {
            if (_registeredCallbackProxy)
            {
                return;
            }

            lock (_callbackLock)
            {
                if (_registeredCallbackProxy)
                {
                    return;
                }

                _registeredCallbackProxy = true;
                _cancellationTokenSource = new CancellationTokenSource();
                _disposables = new List<IDisposable>();
                for (var i = 0; i < ChangeTokens.Count; i++)
                {
                    if (ChangeTokens[i].ActiveChangeCallbacks)
                    {
                        var disposable = ChangeTokens[i].RegisterChangeCallback(_onChangeDelegate, this);
                        _disposables.Add(disposable);
                    }
                }
            }
        }

        private static void OnChange(object state)
        {
            var compositeChangeTokenState = (CompositeChangeToken)state;
            lock (compositeChangeTokenState._callbackLock)
            {
                try
                {
                    compositeChangeTokenState._cancellationTokenSource.Cancel();
                }

                catch (Exception)
                {
                }
            }

            foreach (var disposable in compositeChangeTokenState._disposables)
            {
                disposable.Dispose();
            }
        }
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// Propagates notifications that a change has occurred.
    /// </summary>
    public static class ChangeToken
    {
        /// <summary>
        /// Registers the <paramref name="changeTokenConsumer"/> action to be called whenever the token produced changes.
        /// </summary>
        /// <param name="changeTokenProducer">Produces the change token.</param>
        /// <param name="changeTokenConsumer">Action called when the token changes.</param>
        /// <returns></returns>
        public static IDisposable OnChange(Func<IChangeToken> changeTokenProducer, Action changeTokenConsumer)
        {
            if (changeTokenProducer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenProducer));
            }
            if (changeTokenConsumer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenConsumer));
            }

            return new ChangeTokenRegistration<Action>(changeTokenProducer, callback => callback(), changeTokenConsumer);
        }

        /// <summary>
        /// Registers the <paramref name="changeTokenConsumer"/> action to be called whenever the token produced changes.
        /// </summary>
        /// <param name="changeTokenProducer">Produces the change token.</param>
        /// <param name="changeTokenConsumer">Action called when the token changes.</param>
        /// <param name="state">state for the consumer.</param>
        /// <returns></returns>
        public static IDisposable OnChange<TState>(Func<IChangeToken> changeTokenProducer, Action<TState> changeTokenConsumer, TState state)
        {
            if (changeTokenProducer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenProducer));
            }
            if (changeTokenConsumer == null)
            {
                throw new ArgumentNullException(nameof(changeTokenConsumer));
            }

            return new ChangeTokenRegistration<TState>(changeTokenProducer, changeTokenConsumer, state);
        }

        private class ChangeTokenRegistration<TState> : IDisposable
        {
            private readonly Func<IChangeToken> _changeTokenProducer;
            private readonly Action<TState> _changeTokenConsumer;
            private readonly TState _state;
            private object _disposable;

            private static readonly object _disposedSentinel = new object();

            public ChangeTokenRegistration(Func<IChangeToken> changeTokenProducer, Action<TState> changeTokenConsumer, TState state)
            {
                _changeTokenProducer = changeTokenProducer;
                _changeTokenConsumer = changeTokenConsumer;
                _state = state;

                var token = changeTokenProducer();

                RegisterChangeTokenCallback(token);
            }

            private void OnChangeTokenFired()
            {
                // The order here is important. We need to take the token and then apply our changes BEFORE
                // registering. This prevents us from possible having two change updates to process concurrently.
                //
                // If the token changes after we take the token, then we'll process the update immediately upon
                // registering the callback.
                var token = _changeTokenProducer();

                try
                {
                    _changeTokenConsumer(_state);
                }
                finally
                {
                    // We always want to ensure the callback is registered
                    RegisterChangeTokenCallback(token);
                }
            }

            private void RegisterChangeTokenCallback(IChangeToken token)
            {
                var registraton = token.RegisterChangeCallback(s => ((ChangeTokenRegistration<TState>)s).OnChangeTokenFired(), this);

                SetDisposable(registraton);
            }

            private void SetDisposable(IDisposable disposable)
            {
                // We don't want to transition from _disposedSentinel => anything since it's terminal
                // but we want to allow going from previously assigned disposable, to another
                // disposable. The assumption here is that we don't have to dispose the registration if an IChangeToken
                // fires.
                var current = Volatile.Read(ref _disposable);

                var previous = Interlocked.CompareExchange(ref _disposable, disposable, current);

                if (previous == _disposedSentinel)
                {
                    // The subscription was disposed so we dispose immediately and return
                    disposable.Dispose();
                }
                else if (previous == current)
                {
                    // We successfuly assigned the _disposable field to disposable
                }
                else
                {
                    // Sets can never overlap with other sets so we should never get into this situation
                    Debug.Fail("Somebody else set the _disposable");
                }
            }

            public void Dispose()
            {
                // If the previous value is disposable then dispose it, otherwise,
                // now we've set the disposed sentinel
                if (Interlocked.Exchange(ref _disposable, _disposedSentinel) is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}

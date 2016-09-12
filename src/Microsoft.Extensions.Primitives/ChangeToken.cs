// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// Propagates notifications that a change has occured.
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

            Action<object> callback = null;
            callback = (s) =>
            {
                // The order here is important. We need to take the token and then apply our changes BEFORE
                // registering. This prevents us from possible having two change updates to process concurrently.
                //
                // If the token changes after we take the token, then we'll process the update immediately upon
                // registering the callback.
                var t = changeTokenProducer();
                try
                {
                    changeTokenConsumer();
                }
                catch // Uncaught exceptions are really bad here
                { }
                t.RegisterChangeCallback(callback, null);
            };

            return changeTokenProducer().RegisterChangeCallback(callback, null);
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

            Action<object> callback = null;
            callback = (s) =>
            {
                // The order here is important. We need to take the token and then apply our changes BEFORE
                // registering. This prevents us from possible having two change updates to process concurrently.
                //
                // If the token changes after we take the token, then we'll process the update immediately upon
                // registering the callback.
                var t = changeTokenProducer();
                try
                {
                    changeTokenConsumer((TState)s);
                }
                catch // Uncaught exceptions are really bad here
                { }
                t.RegisterChangeCallback(callback, s);
            };

            return changeTokenProducer().RegisterChangeCallback(callback, state);
        }
    }
}
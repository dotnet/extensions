// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// Represents a composition of <see cref="IChangeToken"/>.
    /// </summary>
    public class CompositeChangeToken : IChangeToken
    {
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

        public IReadOnlyList<IChangeToken> ChangeTokens { get; }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            var disposables = new IDisposable[ChangeTokens.Count];
            for (var i = 0; i < ChangeTokens.Count; i++)
            {
                var disposable = ChangeTokens[i].RegisterChangeCallback(callback, state);
                disposables[i] = disposable;
            }
            return new CompositeDisposable(disposables);
        }

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

        private class CompositeDisposable : IDisposable
        {
            private readonly IDisposable[] _disposables;

            public CompositeDisposable(IDisposable[] disposables)
            {
                _disposables = disposables;
            }

            public void Dispose()
            {
                for (var i = 0; i < _disposables.Length; i++)
                {
                    _disposables[i].Dispose();
                }
            }
        }
    }
}

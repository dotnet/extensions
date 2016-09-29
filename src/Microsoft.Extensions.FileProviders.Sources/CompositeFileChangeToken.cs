// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// Represents a composition of <see cref="IChangeToken"/>.
    /// </summary>
    internal class CompositeFileChangeToken : IChangeToken
    {
        /// <summary>
        /// Creates a new instance of <see cref="CompositeFileChangeToken"/>.
        /// </summary>
        /// <param name="changeTokens">The list of <see cref="IChangeToken"/> to compose.</param>
        public CompositeFileChangeToken(IList<IChangeToken> changeTokens)
        {
            if (changeTokens == null)
            {
                throw new ArgumentNullException(nameof(changeTokens));
            }

            ChangeTokens = changeTokens;
        }

        public IList<IChangeToken> ChangeTokens { get; }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            var disposables = new List<IDisposable>(ChangeTokens.Count);
            for (var i = 0; i < ChangeTokens.Count; i++)
            {
                var disposable = ChangeTokens[i].RegisterChangeCallback(callback, state);
                disposables.Add(disposable);
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
    }
}
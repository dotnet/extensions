// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders
{
    /// <summary>
    /// Represents a composition of <see cref="IChangeToken"/>.
    /// </summary>
    internal class CompositeFileChangeToken : IChangeToken
    {
        private readonly IList<IChangeToken> _changeTokens;

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
            _changeTokens = changeTokens;
        }

        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
            var disposables = new List<IDisposable>();
            for (var i = 0; i < _changeTokens.Count; i++)
            {
                var changeToken = _changeTokens[i];
                var disposable = _changeTokens[i].RegisterChangeCallback(callback, state);
                disposables.Add(disposable);
            }
            return new CompositeDisposable(disposables);
        }

        public bool HasChanged
        {
            get { return _changeTokens.Any(token => token.HasChanged); }
        }

        public bool ActiveChangeCallbacks
        {
            get { return _changeTokens.Any(token => token.ActiveChangeCallbacks); }
        }
    }
}
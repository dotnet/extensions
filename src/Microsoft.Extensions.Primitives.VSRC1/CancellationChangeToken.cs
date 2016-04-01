// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.Extensions.Primitives.VSRC1
{
    /// <summary>
    /// A <see cref="IChangeToken"/> implementation using <see cref="CancellationToken"/>.
    /// </summary>
    public class CancellationChangeToken : IChangeToken
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CancellationChangeToken"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public CancellationChangeToken(CancellationToken cancellationToken)
        {
            Token = cancellationToken;
        }

        /// <inheritdoc />
        public bool ActiveChangeCallbacks => true;

        /// <inheritdoc />
        public bool HasChanged => Token.IsCancellationRequested;

        private CancellationToken Token { get; }

        /// <inheritdoc />
        public IDisposable RegisterChangeCallback(Action<object> callback, object state) => Token.Register(callback, state);
    }
}
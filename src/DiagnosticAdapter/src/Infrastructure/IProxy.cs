// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.DiagnosticAdapter.Infrastructure
{
    /// <summary>
    /// An interface for unwrappable proxy objects.
    /// </summary>
    public interface IProxy
    {
        /// <summary>
        /// Unwraps the underlying object and performs a cast to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the underlying object.</typeparam>
        /// <returns>The underlying object.</returns>
        T Upwrap<T>();
    }
}

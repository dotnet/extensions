// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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

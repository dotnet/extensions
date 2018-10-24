// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.DiagnosticAdapter.Infrastructure
{
    /// <summary>
    /// A factory for runtime creation of proxy objects.
    /// </summary>
    public interface IProxyFactory
    {
        /// <summary>
        /// Creates a proxy object that is assignable to type <typeparamref name="TProxy"/>
        /// </summary>
        /// <typeparam name="TProxy">The type of the proxy to create.</typeparam>
        /// <param name="obj">The object to wrap in a proxy.</param>
        /// <returns>A proxy object, or <paramref name="obj"/> if a proxy is not needed.</returns>
        TProxy CreateProxy<TProxy>(object obj);
    }
}

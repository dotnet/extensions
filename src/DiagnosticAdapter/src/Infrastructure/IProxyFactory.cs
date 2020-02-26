// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

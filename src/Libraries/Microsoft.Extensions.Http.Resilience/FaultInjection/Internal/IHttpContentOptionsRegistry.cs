// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection.Internal;

/// <summary>
/// The interface of a registry class implementation for <see cref="HttpContentOptions"/>
/// registration and retrieval.
/// </summary>
internal interface IHttpContentOptionsRegistry
{
    /// <summary>
    /// Get an instance of <see cref="HttpContent"/> from a registered <see cref="HttpContentOptions"/> by key.
    /// </summary>
    /// <param name="key">The identifier.</param>
    /// <returns>
    /// An instance of <see cref="HttpContent"/> from the registered <see cref="HttpContentOptions"/>
    /// instance identified by the given key.
    /// Returns <see langword="null"/> if the provided key is <see langword="null"/> or not found.
    /// </returns>
    public HttpContent? GetHttpContent(string key);
}

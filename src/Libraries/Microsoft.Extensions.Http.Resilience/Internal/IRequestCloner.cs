// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// Internal interface for cloning an <see cref="HttpRequestMessage"/> instance.
/// </summary>
internal interface IRequestCloner
{
    /// <summary>
    /// Creates a snapshot of <paramref name="request"/> that can be then used for cloning.
    /// </summary>
    /// <param name="request">The request message.</param>
    /// <returns>The snapshot instance.</returns>
    IHttpRequestMessageSnapshot CreateSnapshot(HttpRequestMessage request);
}

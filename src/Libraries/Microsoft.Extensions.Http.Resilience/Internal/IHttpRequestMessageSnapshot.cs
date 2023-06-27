// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// The snapshot of <see cref="HttpRequestMessage"/> created by <see cref="IRequestCloner"/>.
/// </summary>
internal interface IHttpRequestMessageSnapshot : IDisposable
{
    /// <summary>
    /// Creates a new instance of <see cref="HttpRequestMessage"/> from the snapshot.
    /// </summary>
    /// <returns>A <see cref="HttpRequestMessage"/> instance.</returns>
    HttpRequestMessage Create();
}

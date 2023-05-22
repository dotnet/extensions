// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// Extension methods for the <see cref="HttpResponseMessage"/>.
/// </summary>
internal static class HttpRequestMessageExtensions
{
    /// <summary>
    /// Replaces the base URI of an <see cref="HttpResponseMessage"/>.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="updatedUri">The updated URI.</param>
    /// <returns>Incoming <see cref="HttpRequestMessage"/> with new <see cref="Uri"/>.</returns>
    public static HttpRequestMessage ReplaceHost(this HttpRequestMessage request, Uri updatedUri)
    {
        var replacedUri = request.RequestUri!.ReplaceHost(updatedUri);
        request.RequestUri = replacedUri;

        return request;
    }
}

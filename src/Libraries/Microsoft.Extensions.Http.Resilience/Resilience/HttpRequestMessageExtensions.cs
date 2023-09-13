// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// The resilience extensions for <see cref="HttpRequestMessage"/>.
/// </summary>
public static class HttpRequestMessageExtensions
{
#if NET6_0_OR_GREATER
    private static readonly HttpRequestOptionsKey<ResilienceContext?> _resilienceContextKey = new("Resilience.Http.ResilienceContext");
#else
    private const string ResilienceContextKey = "Resilience.Http.ResilienceContext";
#endif

    /// <summary>
    /// Gets the <see cref="ResilienceContext"/> from the request message.
    /// </summary>
    /// <param name="requestMessage">The request.</param>
    /// <returns>An instance of <see cref="ResilienceContext"/> or <see langword="null"/>.</returns>
    public static ResilienceContext? GetResilienceContext(this HttpRequestMessage requestMessage)
    {
        _ = Throw.IfNull(requestMessage);

#if NET6_0_OR_GREATER
        if (requestMessage.Options.TryGetValue(_resilienceContextKey, out var context))
        {
            return context;
        }
#else
        if (requestMessage.Properties.TryGetValue(ResilienceContextKey, out var contextRaw) && contextRaw is ResilienceContext context)
        {
            return context;
        }
#endif

        return null;
    }

    /// <summary>
    /// Sets the <see cref="ResilienceContext"/> on the request message.
    /// </summary>
    /// <param name="requestMessage">The request.</param>
    /// <param name="resilienceContext">An instance of <see cref="ResilienceContext"/>.</param>
    public static void SetResilienceContext(this HttpRequestMessage requestMessage, ResilienceContext? resilienceContext)
    {
        _ = Throw.IfNull(requestMessage);
#if NET6_0_OR_GREATER
        requestMessage.Options.Set(_resilienceContextKey, resilienceContext);
#else
        requestMessage.Properties[ResilienceContextKey] = resilienceContext;
#endif
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Polly;

/// <summary>
/// Provides utility methods for working with <see cref="ResilienceContext"/>.
/// </summary>
public static class HttpResilienceContextExtensions
{
    /// <summary>
    /// Gets the request message from the <see cref="ResilienceContext"/>.
    /// </summary>
    /// <param name="context">The resilience context.</param>
    /// <returns>
    /// The request message.
    /// If the request message is not present in the <see cref="ResilienceContext"/> the method returns <see langword="null"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static HttpRequestMessage? GetRequestMessage(this ResilienceContext context)
    {
        _ = Throw.IfNull(context);
        return context.Properties.GetValue(ResilienceKeys.RequestMessage, default);
    }

    /// <summary>
    /// Sets the request message on the <see cref="ResilienceContext"/>.
    /// </summary>
    /// <param name="context">The resilience context.</param>
    /// <param name="requestMessage">The request message.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static void SetRequestMessage(this ResilienceContext context, HttpRequestMessage? requestMessage)
    {
        _ = Throw.IfNull(context);
        context.Properties.Set(ResilienceKeys.RequestMessage, requestMessage);
    }
}

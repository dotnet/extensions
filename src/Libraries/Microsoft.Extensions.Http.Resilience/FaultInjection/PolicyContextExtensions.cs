// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.FaultInjection;

/// <summary>
/// Provides extension methods for <see cref="Polly.Context"/>.
/// </summary>
[Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class PolicyContextExtensions
{
    private const string CallingRequestMessage = "CallingRequestMessage";

    /// <summary>
    /// Associates the given <see cref="Context"/> instance to the <paramref name="request"/>.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <param name="request">The calling request.</param>
    /// <returns>
    /// The <see cref="Context"/> so that additional calls can be chained.
    /// </returns>
    public static Context WithCallingRequestMessage(this Context context, HttpRequestMessage request)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(request);

        context[CallingRequestMessage] = request;

        return context;
    }

    internal static HttpRequestMessage? GetCallingRequestMessage(this Context context)
    {
        _ = Throw.IfNull(context);

        if (context.TryGetValue(CallingRequestMessage, out var contextObj))
        {
            if (contextObj is HttpRequestMessage request)
            {
                return request;
            }
        }

        return null;
    }
}

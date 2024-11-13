// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Extensions for <see cref="HttpRetryStrategyOptions"/>.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Resilience, UrlFormat = DiagnosticIds.UrlFormat)]
public static class HttpRetryStrategyOptionsExtensions
{
#if !NET8_0_OR_GREATER
    private static readonly HttpMethod _connect = new("CONNECT");
    private static readonly HttpMethod _patch = new("PATCH");
#endif

    /// <summary>
    /// Disables retry attempts for POST, PATCH, PUT, DELETE, and CONNECT HTTP methods.
    /// </summary>
    /// <param name="options">The retry strategy options.</param>
    public static void DisableForUnsafeHttpMethods(this HttpRetryStrategyOptions options)
    {
        options.DisableFor(
            HttpMethod.Delete, HttpMethod.Post, HttpMethod.Put,
#if !NET8_0_OR_GREATER
            _connect, _patch);
#else
            HttpMethod.Connect, HttpMethod.Patch);
#endif
    }

    /// <summary>
    /// Disables retry attempts for the given list of HTTP methods.
    /// </summary>
    /// <param name="options">The retry strategy options.</param>
    /// <param name="methods">The list of HTTP methods.</param>
    public static void DisableFor(this HttpRetryStrategyOptions options, params HttpMethod[] methods)
    {
        _ = Throw.IfNull(options);
        _ = Throw.IfNullOrEmpty(methods);

        var shouldHandle = options.ShouldHandle;

        options.ShouldHandle = async args =>
        {
            var result = await shouldHandle(args).ConfigureAwait(args.Context.ContinueOnCapturedContext);

            if (result &&
                args.Outcome.Result is HttpResponseMessage response &&
                response.RequestMessage is HttpRequestMessage request)
            {
                return !methods.Contains(request.Method);
            }

            return result;
        };
    }
}


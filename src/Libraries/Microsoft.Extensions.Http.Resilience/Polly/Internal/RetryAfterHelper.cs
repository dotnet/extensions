// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using Microsoft.Extensions.Resilience.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class RetryAfterHelper
{
    public static TimeSpan Generator(RetryDelayArguments<HttpResponseMessage> args)
    {
        if (args.Result?.Result is HttpResponseMessage response)
        {
            return ParseRetryAfterHeader(response, TimeProvider.System);
        }

        return TimeSpan.Zero;
    }

    /// <summary>
    /// Parses Retry-After value from the relevant HTTP response header.
    /// If not found then it will return <see cref="TimeSpan.Zero" />.
    /// </summary>
    /// <param name="httpResponse">HTTP response message.</param>
    /// <param name="timeProvider">Current time provider for conversion of absolute values.</param>
    /// <returns>The delay according to the Retry-After header.</returns>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Retry-After" />.
    internal static TimeSpan ParseRetryAfterHeader(HttpResponseMessage httpResponse, TimeProvider timeProvider)
    {
        var headers = httpResponse?.Headers;
        if (headers?.RetryAfter != null)
        {
            if (headers.RetryAfter.Date.HasValue)
            {
                // An absolute point in time
                return headers.RetryAfter.Date.Value - timeProvider.GetUtcNow();
            }
            else if (headers.RetryAfter.Delta.HasValue)
            {
                // A relative number of seconds
                return headers.RetryAfter.Delta.Value;
            }
        }

        return TimeSpan.Zero;
    }
}

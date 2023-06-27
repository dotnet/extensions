// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class RetryAfterHelper
{
    /// <summary>
    /// Parses Retry-After value from the relevant HTTP response header.
    /// If not found then it will return <see cref="TimeSpan.Zero" />.
    /// </summary>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Retry-After" />.
    internal static bool TryParse(HttpResponseMessage response, TimeProvider timeProvider, out TimeSpan retryAfter)
    {
        var (parsed, delay) = response.Headers.RetryAfter switch
        {
            { Date: { } date } => (true, date - timeProvider.GetUtcNow()),
            { Delta: { } delta } => (true, delta),
            _ => (false, default)
        };

        // It can happen that the server returns a point in time in the past.
        // This indicates that retry can happen immediately.
        if (parsed && delay < TimeSpan.Zero)
        {
            delay = TimeSpan.Zero;
        }

        retryAfter = delay;
        return parsed;
    }
}

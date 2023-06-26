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
        retryAfter = response.Headers.RetryAfter switch
        {
            { Date: { } date } => date - timeProvider.GetUtcNow(),
            { Delta: { } delta } => delta,
            _ => TimeSpan.MinValue
        };

        return retryAfter >= TimeSpan.Zero;
    }
}

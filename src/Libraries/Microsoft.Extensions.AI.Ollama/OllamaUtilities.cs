// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;

namespace Microsoft.Extensions.AI;

internal static class OllamaUtilities
{
    /// <summary>Gets a singleton <see cref="HttpClient"/> used when no other instance is supplied.</summary>
    public static HttpClient SharedClient { get; } = new()
    {
        // Expected use is localhost access for non-production use. Typical production use should supply
        // an HttpClient configured with whatever more robust resilience policy / handlers are appropriate.
        Timeout = Timeout.InfiniteTimeSpan,
    };

    public static void TransferNanosecondsTime<TResponse>(TResponse response, Func<TResponse, long?> getNanoseconds, string key, ref AdditionalPropertiesDictionary<long>? metadata)
    {
        if (getNanoseconds(response) is long duration)
        {
            try
            {
                (metadata ??= [])[key] = duration;
            }
            catch (OverflowException)
            {
                // Ignore options that don't convert
            }
        }
    }
}

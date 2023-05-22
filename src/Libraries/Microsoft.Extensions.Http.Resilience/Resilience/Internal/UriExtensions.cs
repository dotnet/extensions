// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// Extensions for Uri class to replace host.
/// </summary>
internal static class UriExtensions
{
#if !NETCOREAPP3_1_OR_GREATER
    private static readonly char[] _questionMark = new[] { '?' };
#endif

    /// <summary>
    /// Replaces host of <paramref name="currentUri"/> with the host from <paramref name="updatedUri"/>.
    /// </summary>
    /// <param name="currentUri">Uri with old host.</param>
    /// <param name="updatedUri">Uri with new host.</param>
    /// <returns><paramref name="currentUri"/> with host from <paramref name="updatedUri"/>.</returns>
    public static Uri ReplaceHost(this Uri currentUri, Uri updatedUri)
    {
        _ = Throw.IfNull(currentUri);
        _ = Throw.IfNull(updatedUri);

        var builder = new UriBuilder(updatedUri)
        {
            Path = currentUri.LocalPath,

            // UriBuilder always prepends with a question mark when setting the Query property.
#if NETCOREAPP3_1_OR_GREATER
            Query = currentUri.Query.TrimStart('?')
#else
            Query = currentUri.Query.TrimStart(_questionMark)
#endif
        };

        return builder.Uri;
    }
}

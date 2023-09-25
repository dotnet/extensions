// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

internal static class MediaTypeSetExtensions
{
    public static bool Covers(this MediaType[] supportedMediaTypes, string? mediaTypeToCheck)
    {
        if (string.IsNullOrEmpty(mediaTypeToCheck))
        {
            return false;
        }

        var sampleContentType = new MediaType(mediaTypeToCheck);
        foreach (var supportedMediaType in supportedMediaTypes)
        {
            if (sampleContentType.IsSubsetOf(supportedMediaType))
            {
                return true;
            }
        }

        return false;
    }
}

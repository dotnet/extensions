// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI.Evaluation.Safety;

internal static class AIContentExtensions
{
    internal static bool IsTextOrUsage(this AIContent content)
        => content is TextContent || content is UsageContent;

    internal static bool IsImageWithSupportedFormat(this AIContent content) =>
        (content is UriContent uriContent && IsSupportedImageFormat(uriContent.MediaType)) ||
        (content is DataContent dataContent && IsSupportedImageFormat(dataContent.MediaType));

    private static bool IsSupportedImageFormat(string mediaType)
    {
        // 'image/jpeg' is the official MIME type for JPEG. However, some systems recognize 'image/jpg' as well.

        return
            mediaType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase) ||
            mediaType.Equals("image/jpg", StringComparison.OrdinalIgnoreCase) ||
            mediaType.Equals("image/png", StringComparison.OrdinalIgnoreCase) ||
            mediaType.Equals("image/gif", StringComparison.OrdinalIgnoreCase);
    }
}

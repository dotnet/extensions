// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI.Evaluation.Safety;
internal static class AIContentExtensions
{
    internal static bool IsTextOrUsage(this AIContent content)
        => content is TextContent || content is UsageContent;

    internal static bool IsImage(this AIContent content) =>
        (content is UriContent uriContent && uriContent.HasTopLevelMediaType("image")) ||
        (content is DataContent dataContent && dataContent.HasTopLevelMediaType("image"));

    internal static bool IsUriBase64Encoded(this DataContent dataContent)
    {
        ReadOnlyMemory<char> uri = dataContent.Uri.AsMemory();

        int commaIndex = uri.Span.IndexOf(',');
        if (commaIndex == -1)
        {
            return false;
        }

        ReadOnlyMemory<char> metadata = uri.Slice(0, commaIndex);

        bool isBase64Encoded = metadata.Span.EndsWith(";base64".AsSpan(), StringComparison.OrdinalIgnoreCase);
        return isBase64Encoded;
    }
}

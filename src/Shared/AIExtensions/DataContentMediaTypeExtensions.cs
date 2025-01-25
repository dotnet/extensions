// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for interpreting the media type of a <see cref="DataContent"/>.
/// </summary>
#if !SHARED_PROJECT
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
#endif
internal static class DataContentMediaTypeExtensions
{
    /// <summary>
    /// Determines whether the <see cref="DataContent"/> represents an image.
    /// </summary>
    /// <param name="content">The <see cref="DataContent"/>.</param>
    /// <returns><see langword="true"/> if the content represents an image; otherwise, <see langword="false"/>.</returns>
    public static bool HasImageMediaType(this DataContent content)
        => content.MediaType is { } mediaType && mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}

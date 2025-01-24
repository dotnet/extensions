// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

internal static class DataContentMediaTypeExtensions
{
    public static bool HasImageMediaType(this DataContent content)
        => content.MediaType is { } mediaType && mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}

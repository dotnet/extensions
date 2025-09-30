// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Latency.Internal;

internal static class HttpTags
{
    public const string HttpVersion = "httpver";

    public static readonly string[] Tags =
    [
        HttpVersion
    ];
}

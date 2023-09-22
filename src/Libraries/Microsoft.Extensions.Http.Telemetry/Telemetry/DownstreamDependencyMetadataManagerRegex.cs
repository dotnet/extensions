// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Microsoft.Extensions.Telemetry;

#if NET7_0_OR_GREATER
internal static partial class DownstreamDependencyMetadataManagerRegex
#else
internal static class DownstreamDependencyMetadataManagerRegex
#endif
{
    private const string RouteRegexString = @"(\{[^\}]+\})+";

#if NET7_0_OR_GREATER

    [GeneratedRegex(RouteRegexString)]
    public static partial Regex MakeRouteRegex();

#else

    public static Regex MakeRouteRegex() => new(RouteRegexString, RegexOptions.Compiled);

#endif
}

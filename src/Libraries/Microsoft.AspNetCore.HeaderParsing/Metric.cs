// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.HeaderParsing;

internal static partial class Metric
{
    [Counter("HeaderName", "Kind", Name = @"HeaderParsing.ParsingErrors")]
    public static partial ParsingErrorCounter CreateParsingErrorCounter(Meter meter);

    [Counter("HeaderName", "Type", Name = @"HeaderParsing.CacheAccess")]
    public static partial CacheAccessCounter CreateCacheAccessCounter(Meter meter);
}

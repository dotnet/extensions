﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.Metrics;

namespace Microsoft.AspNetCore.HeaderParsing;

internal static partial class Metric
{
    [Counter("aspnetcore.header_parsing.header.name", "error.type", Name = "aspnetcore.header_parsing.parse_errors")]
    public static partial ParsingErrorCounter CreateParsingErrorCounter(Meter meter);

    [Counter("aspnetcore.header_parsing.header.name", "aspnetcore.header_parsing.cache_access.type", Name = "aspnetcore.header_parsing.cache_accesses")]
    public static partial CacheAccessCounter CreateCacheAccessCounter(Meter meter);
}

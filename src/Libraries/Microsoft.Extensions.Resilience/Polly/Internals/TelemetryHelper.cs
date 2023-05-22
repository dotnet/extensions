// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.Extensions.Resilience.Internal;

internal static class TelemetryHelper
{
    internal const string DefaultDimensionValue = "Undefined";

    internal static string GetDimensionOrDefault(this string? dimension)
    {
        return string.IsNullOrEmpty(dimension) ? DefaultDimensionValue : dimension!;
    }

    internal static string GetDimensionOrUnknown(this string? dimension)
    {
        return string.IsNullOrEmpty(dimension) ? TelemetryConstants.Unknown : dimension!;
    }
}

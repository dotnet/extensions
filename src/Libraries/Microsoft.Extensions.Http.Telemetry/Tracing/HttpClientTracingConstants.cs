// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

internal static class HttpClientTracingConstants
{
    /// <summary>
    /// Key used to get/set dependency name in HTTP request message options/properties.
    /// We use "R9-" prefix to avoid collisions.
    /// </summary>
    public const string DependencyNameKey = "R9-DependencyName";

    /// <summary>
    /// Key used to get/set request name in HTTP request message options/properties.
    /// We use "R9-" prefix to avoid collisions.
    /// </summary>
    public const string RequestRouteKey = "R9-RequestRoute";
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Telemetry.Metering.Internal;

/// <summary>
/// HTTP client metering constants.
/// </summary>
internal static class HttpClientMeteringConstants
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
    public const string RequestNameKey = "R9-RequestName";
}

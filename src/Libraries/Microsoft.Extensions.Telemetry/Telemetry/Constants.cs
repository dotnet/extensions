// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.Extensions.Telemetry;

/// <summary>
/// Common telemetry constants used by various telemetry libraries.
/// </summary>
internal static class Constants
{
    public const int ASCIICharCount = 128;

    public const char DefaultRouteEndDelim = '?';

    public static class HttpWebConstants
    {
        /// <summary>
        /// Request Route HTTP Header key.
        /// </summary>
        public const string RequestRouteHeader = $"X-{TelemetryConstants.RequestMetadataKey}-{nameof(RequestMetadata.RequestRoute)}";

        /// <summary>
        /// Request Name HTTP Header key.
        /// </summary>
        public const string RequestNameHeader = $"X-{TelemetryConstants.RequestMetadataKey}-{nameof(RequestMetadata.RequestName)}";

        /// <summary>
        /// Dependency Name HTTP Header key.
        /// </summary>
        public const string DependencyNameHeader = $"X-{TelemetryConstants.RequestMetadataKey}-{nameof(RequestMetadata.DependencyName)}";
    }
}

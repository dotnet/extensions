// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience.Resilience;

/// <summary>
/// Extensions for <see cref="ResilienceContext"/>.
/// </summary>
public static class ResilienceContextExtensions
{
    private static readonly ResiliencePropertyKey<RequestMetadata?> _requestMetadataKey = new(TelemetryConstants.RequestMetadataKey);

    /// <summary>
    /// Sets the <see cref="RequestMetadata"/> to the <see cref="ResilienceContext"/>.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <param name="requestMetadata">The request metadata.</param>
    public static void SetRequestMetadata(this ResilienceContext context, RequestMetadata requestMetadata)
    {
        _ = Throw.IfNull(context);
        _ = Throw.IfNull(requestMetadata);

        context.Properties.Set(_requestMetadataKey, requestMetadata);
    }

    /// <summary>
    /// Gets the <see cref="RequestMetadata"/> from the <see cref="ResilienceContext"/>.
    /// </summary>
    /// <param name="context">The context instance.</param>
    /// <returns>The instance of <see cref="RequestMetadata"/> or <see langword="null"/>.</returns>
    public static RequestMetadata? GetRequestMetadata(this ResilienceContext context)
    {
        _ = Throw.IfNull(context);

        return context.Properties.GetValue(_requestMetadataKey, null);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Diagnostics.Latency;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions to register the request latency telemetry middleware.
/// </summary>
public static class RequestLatencyTelemetryApplicationBuilderExtensions
{
    /// <summary>
    /// Registers middleware for request checkpointing.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    public static IApplicationBuilder UseRequestCheckpoint(this IApplicationBuilder builder)
        => Throw.IfNull(builder)
            .UseMiddleware<AddServerTimingHeaderMiddleware>([])
            .UseMiddleware<CaptureResponseTimeMiddleware>([])
            .UseMiddleware<CapturePipelineExitMiddleware>([]);

    /// <summary>
    /// Adds the request latency telemetry middleware to <see cref="IApplicationBuilder"/> request execution pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The value of <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null" />.</exception>
    public static IApplicationBuilder UseRequestLatencyTelemetry(this IApplicationBuilder builder)
        => Throw.IfNull(builder)
        .UseMiddleware<RequestLatencyTelemetryMiddleware>([]);
}

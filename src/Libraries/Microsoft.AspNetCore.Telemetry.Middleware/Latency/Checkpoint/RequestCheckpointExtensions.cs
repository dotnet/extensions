// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Extensions used to register Request Checkpoint feature.
/// </summary>
public static class RequestCheckpointExtensions
{
    /// <summary>
    /// Adds all Request Checkpoint services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection AddRequestCheckpoint(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);
        _ = services.AddSingleton<CaptureResponseTimeMiddleware>();
        _ = services.AddSingleton<AddServerTimingHeaderMiddleware>();
        _ = services.AddSingleton<CapturePipelineEntryMiddleware>();
        _ = services.AddPipelineEntryCheckpoint();
        _ = services.AddSingleton<CapturePipelineExitMiddleware>();
        _ = services.RegisterCheckpointNames(RequestCheckpointConstants.RequestCheckpointNames);

        return services;
    }

    /// <summary>
    /// Registers Request Checkpoint related middlewares into the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="IApplicationBuilder"/>.</param>
    /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
    public static IApplicationBuilder UseRequestCheckpoint(this IApplicationBuilder builder)
    {
        _ = Throw.IfNull(builder);

        // the order DOES matter
        _ = builder.UseMiddleware<AddServerTimingHeaderMiddleware>(Array.Empty<object>());
        _ = builder.UseMiddleware<CaptureResponseTimeMiddleware>(Array.Empty<object>());
        _ = builder.UseMiddleware<CapturePipelineExitMiddleware>(Array.Empty<object>());

        return builder;
    }

    /// <summary>
    /// Adds <see cref="CapturePipelineEntryMiddleware"/> at the beginning of the middleware pipeline using <see cref="IStartupFilter"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    internal static IServiceCollection AddPipelineEntryCheckpoint(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, CapturePipelineEntryStartupFilter>());

        return services;
    }
}

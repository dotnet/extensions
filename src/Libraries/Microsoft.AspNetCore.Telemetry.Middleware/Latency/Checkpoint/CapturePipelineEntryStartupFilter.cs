// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Adds <see cref="CapturePipelineEntryMiddleware"/> at the beginning of the middleware pipeline.
/// </summary>
internal sealed class CapturePipelineEntryStartupFilter : IStartupFilter
{
    /// <summary>
    /// Wraps the <see cref="IApplicationBuilder"/> directly adds
    /// <see cref="CapturePipelineEntryMiddleware"/> at the beginning the middleware pipeline.
    /// </summary>
    /// <param name="next">The Configure method to extend.</param>
    /// <returns>A modified <see cref="Action"/>.</returns>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            _ = builder.UseMiddleware<CapturePipelineEntryMiddleware>(Array.Empty<object>());
            next(builder);
        };
    }
}

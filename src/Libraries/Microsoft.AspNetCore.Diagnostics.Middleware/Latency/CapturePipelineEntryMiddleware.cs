// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Diagnostics.Latency;

/// <summary>
/// Middleware that should be put at the beginning of the middleware pipeline to capture time.
/// </summary>
internal sealed class CapturePipelineEntryMiddleware
{
    private readonly CheckpointToken _elapsedTillEntry;
    private readonly RequestDelegate _next;

    public CapturePipelineEntryMiddleware(RequestDelegate next, ILatencyContextTokenIssuer tokenIssuer)
    {
        _elapsedTillEntry = tokenIssuer.GetCheckpointToken(RequestCheckpointConstants.ElapsedTillEntryMiddleware);
        _next = Throw.IfNull(next);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context).ConfigureAwait(false);

        var latencyContext = context.RequestServices.GetRequiredService<ILatencyContext>();
        latencyContext.AddCheckpoint(_elapsedTillEntry);
    }
}

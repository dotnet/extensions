// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Latency;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.Diagnostics.Latency;

/// <summary>
/// Middleware that should be put at the end of the pipeline to capture time.
/// </summary>
internal sealed class CapturePipelineExitMiddleware
{
    private readonly CheckpointToken _elapsedTillPipelineExit;

    private readonly CheckpointToken _elapsedResponseProcessed;
    private readonly RequestDelegate _next;

    public CapturePipelineExitMiddleware(RequestDelegate next, ILatencyContextTokenIssuer tokenIssuer)
    {
        _elapsedTillPipelineExit = tokenIssuer.GetCheckpointToken(RequestCheckpointConstants.ElapsedTillPipelineExitMiddleware);
        _elapsedResponseProcessed = tokenIssuer.GetCheckpointToken(RequestCheckpointConstants.ElapsedResponseProcessed);
        _next = Throw.IfNull(next);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var latencyContext = context.RequestServices.GetRequiredService<ILatencyContext>();
        latencyContext.AddCheckpoint(_elapsedTillPipelineExit);

        await _next(context).ConfigureAwait(false);

        latencyContext.AddCheckpoint(_elapsedResponseProcessed);
    }
}

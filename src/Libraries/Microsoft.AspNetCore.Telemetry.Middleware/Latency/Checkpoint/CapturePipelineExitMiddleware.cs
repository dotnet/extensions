// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Telemetry.Latency;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Middleware that should be put at the end of the pipeline to capture time.
/// </summary>
internal sealed class CapturePipelineExitMiddleware : IMiddleware
{
    private readonly CheckpointToken _elapsedTillPipelineExit;

    private readonly CheckpointToken _elapsedResponseProcessed;

    public CapturePipelineExitMiddleware(ILatencyContextTokenIssuer tokenIssuer)
    {
        _elapsedTillPipelineExit = tokenIssuer.GetCheckpointToken(RequestCheckpointConstants.ElapsedTillPipelineExitMiddleware);
        _elapsedResponseProcessed = tokenIssuer.GetCheckpointToken(RequestCheckpointConstants.ElapsedResponseProcessed);
    }

    /// <inheritdoc/>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var latencyContext = context.RequestServices.GetRequiredService<ILatencyContext>();
        latencyContext.AddCheckpoint(_elapsedTillPipelineExit);

        await next(context).ConfigureAwait(false);

        latencyContext.AddCheckpoint(_elapsedResponseProcessed);
    }
}

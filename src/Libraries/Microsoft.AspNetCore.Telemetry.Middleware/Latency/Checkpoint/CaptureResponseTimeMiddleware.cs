// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Latency;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// A middleware that captures response times.
/// </summary>
internal sealed class CaptureResponseTimeMiddleware : IMiddleware
{
    private readonly CheckpointToken _elapsedTillHeaders;

    private readonly CheckpointToken _elapsedTillFinished;

    public CaptureResponseTimeMiddleware(ILatencyContextTokenIssuer tokenIssuer)
    {
        _elapsedTillHeaders = tokenIssuer.GetCheckpointToken(RequestCheckpointConstants.ElapsedTillHeaders);
        _elapsedTillFinished = tokenIssuer.GetCheckpointToken(RequestCheckpointConstants.ElapsedTillFinished);
    }

    /// <summary>
    /// Request handling method.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <returns>A <see cref="Task"/> that represents the execution of this middleware.</returns>
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var latencyContext = context.RequestServices.GetRequiredService<ILatencyContext>();

        // Capture the time just before response headers will be sent to the client.
        context.Response.OnStarting(l =>
        {
            var latencyContext = l as ILatencyContext;
            latencyContext!.AddCheckpoint(_elapsedTillHeaders);
            return Task.CompletedTask;
        }, latencyContext);

        // Capture the time after the response has finished being sent to the client.
        context.Response.OnCompleted(l =>
        {
            var latencyContext = l as ILatencyContext;
            latencyContext!.AddCheckpoint(_elapsedTillFinished);
            return Task.CompletedTask;
        }, latencyContext);

        // Call the next delegate/middleware in the pipeline
        return next(context);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Telemetry.Latency;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// A middleware that populates Server-Timing header with response processing time.
/// </summary>
internal sealed class AddServerTimingHeaderMiddleware : IMiddleware
{
    internal const string ServerTimingHeaderName = "Server-Timing";

    /// <summary>
    /// Request handling method.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="next">The delegate representing the remaining middleware in the request pipeline.</param>
    /// <returns>A <see cref="Task"/> that represents the execution of this middleware.</returns>
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        context.Response.OnStarting(ctx =>
        {
            var httpContext = (HttpContext)ctx;
            var latencyContext = httpContext.RequestServices.GetRequiredService<ILatencyContext>();

            if (latencyContext.TryGetCheckpoint(RequestCheckpointConstants.ElapsedTillHeaders, out var timestamp, out var timestampFrequency))
            {
                var elapsedMs = (long)(((double)timestamp / timestampFrequency) * 1000);

                if (httpContext.Response.Headers.TryGetValue(ServerTimingHeaderName, out var existing))
                {
                    httpContext.Response.Headers[ServerTimingHeaderName] = $"{existing}, reqlatency;dur={elapsedMs}";
                }
                else
                {
                    httpContext.Response.Headers.Add(ServerTimingHeaderName, $"reqlatency;dur={elapsedMs}");
                }
            }

            return Task.CompletedTask;
        }, context);

        return next(context);
    }
}

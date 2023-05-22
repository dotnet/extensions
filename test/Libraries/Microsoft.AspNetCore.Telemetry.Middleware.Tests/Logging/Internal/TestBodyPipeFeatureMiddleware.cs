// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Telemetry.Http.Logging.Test.Internal;

internal sealed class TestBodyPipeFeatureMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/err-pipe"))
        {
            context.Features.Set<IRequestBodyPipeFeature>(new RequestBodyErrorPipeFeature());
        }

        if (context.Request.Path.StartsWithSegments("/multi-segment-pipe"))
        {
            context.Features.Set<IRequestBodyPipeFeature>(new RequestBodyMultiSegmentPipeFeature());
        }

        await next(context);
    }
}

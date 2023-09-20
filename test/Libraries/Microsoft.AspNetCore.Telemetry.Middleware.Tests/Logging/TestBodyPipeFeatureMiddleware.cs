// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Diagnostics.Logging.Test;

internal sealed class TestBodyPipeFeatureMiddleware : IMiddleware
{
    public Action? RequestBodyInfinitePipeFeatureCallback { get; set; }

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

        if (context.Request.Path.StartsWithSegments("/infinite-pipe"))
        {
            var infinitePipeFeature = new RequestBodyInfinitePipeFeature(RequestBodyInfinitePipeFeatureCallback);
            context.Features.Set<IRequestBodyPipeFeature>(infinitePipeFeature);
        }

        await next(context);
    }
}

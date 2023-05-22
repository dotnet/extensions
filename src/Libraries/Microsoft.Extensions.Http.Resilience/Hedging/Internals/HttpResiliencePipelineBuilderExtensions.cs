// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience.Internal.Routing;
using Microsoft.Extensions.Resilience.Internal;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class HttpResiliencePipelineBuilderExtensions
{
    public static IHttpResiliencePipelineBuilder AddRequestMessageSnapshotPolicy(this IHttpResiliencePipelineBuilder builder)
    {
        var pipelineName = builder.PipelineName;

        _ = builder.AddPolicy((builder, serviceProvider) => builder.AddPolicy(ActivatorUtilities.CreateInstance<RequestMessageSnapshotPolicy>(serviceProvider, pipelineName)));

        return builder;
    }

    public static IHttpResiliencePipelineBuilder AddRoutingPolicy(
        this IHttpResiliencePipelineBuilder builder,
        Func<IServiceProvider, IRequestRoutingStrategyFactory> factory)
    {
        var pipelineName = builder.PipelineName;

        _ = builder.AddPolicy((builder, serviceProvider) => builder.AddPolicy(new RoutingPolicy(pipelineName, factory(serviceProvider))));

        return builder;
    }
}

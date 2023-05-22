// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience.Internal.Routing;

internal sealed class StandardHedgingHandlerBuilder : IStandardHedgingHandlerBuilder
{
    public StandardHedgingHandlerBuilder(string name, IServiceCollection services, IRoutingStrategyBuilder routingStrategyBuilder, IHttpResiliencePipelineBuilder endpointResiliencePipelineBuilder)
    {
        Name = name;
        Services = services;
        RoutingStrategyBuilder = routingStrategyBuilder;
        EndpointResiliencePipelineBuilder = endpointResiliencePipelineBuilder;
    }

    public string Name { get; }

    public IServiceCollection Services { get; }

    public IRoutingStrategyBuilder RoutingStrategyBuilder { get; }

    public IHttpResiliencePipelineBuilder EndpointResiliencePipelineBuilder { get; }
}

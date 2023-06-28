// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options.Validation;

namespace Microsoft.Extensions.Http.Resilience.Routing.Internal;

internal sealed record RoutingStrategyBuilder : IRoutingStrategyBuilder
{
    public RoutingStrategyBuilder(string name, IServiceCollection services)
    {
        Name = name;
        Services = services;

        _ = Services.AddValidatedOptions<RequestRoutingStrategyOptions, RequestRoutingStrategyOptionsValidator>(name);
    }

    public string Name { get; }

    public IServiceCollection Services { get; }
}

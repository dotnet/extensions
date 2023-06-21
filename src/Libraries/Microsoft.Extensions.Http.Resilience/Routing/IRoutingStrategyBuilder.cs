// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Builder for configuring the routing strategies associated with hedging handler.
/// </summary>
public interface IRoutingStrategyBuilder
{
    /// <summary>
    /// Gets the routing strategy name being configured.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the service collection.
    /// </summary>
    IServiceCollection Services { get; }
}

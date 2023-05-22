// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Defines the builder used to configure the standard hedging handler.
/// </summary>
public interface IStandardHedgingHandlerBuilder
{
    /// <summary>
    /// Gets the name of standard hedging handler being configured.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the service collection.
    /// </summary>
    IServiceCollection Services { get; }

    /// <summary>
    /// Gets the builder for the routing strategy.
    /// </summary>
    IRoutingStrategyBuilder RoutingStrategyBuilder { get; }

    /// <summary>
    /// Gets <see cref="IHttpResiliencePipelineBuilder"/> for endpoint pipeline.
    /// </summary>
    /// <remarks>This property is for internal use only.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    IHttpResiliencePipelineBuilder EndpointResiliencePipelineBuilder { get; }
}

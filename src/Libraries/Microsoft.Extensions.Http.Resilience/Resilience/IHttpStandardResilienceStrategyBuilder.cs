// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// The builder for the standard HTTP resilience strategy.
/// </summary>
public interface IHttpStandardResilienceStrategyBuilder
{
    /// <summary>
    /// Gets the name of the resilience strategy configured by this builder.
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Gets the application service collection.
    /// </summary>
    IServiceCollection Services { get; }
}

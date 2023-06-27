// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Defines a factory for creation of request routing strategies.
/// </summary>
internal interface IRequestRoutingStrategyFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="IRequestRoutingStrategy"/>.
    /// </summary>
    /// <returns>The RequestRoutingStrategy for providing the routes.</returns>
    IRequestRoutingStrategy CreateRoutingStrategy();

    /// <summary>
    /// Returns the strategy instance to the pool.
    /// </summary>
    /// <param name="strategy">The strategy instance.</param>
    void ReturnRoutingStrategy(IRequestRoutingStrategy strategy);
}

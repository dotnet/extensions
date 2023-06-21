// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Resilience.Routing.Internal;

/// <inheritdoc/>
internal interface IPooledRequestRoutingStrategyFactory : IRequestRoutingStrategyFactory
{
    /// <summary>
    /// Returns the strategy instance to the pool.
    /// </summary>
    /// <param name="strategy">The strategy instance.</param>
    void ReturnRoutingStrategy(IRequestRoutingStrategy strategy);
}

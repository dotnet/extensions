// Assembly 'Microsoft.Extensions.Http.Resilience'

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
}

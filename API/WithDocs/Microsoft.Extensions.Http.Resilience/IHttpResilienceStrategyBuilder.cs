// Assembly 'Microsoft.Extensions.Http.Resilience'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// The builder for configuring the HTTP client resilience strategy.
/// </summary>
public interface IHttpResilienceStrategyBuilder
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

// Assembly 'Microsoft.Extensions.Http.Resilience'

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

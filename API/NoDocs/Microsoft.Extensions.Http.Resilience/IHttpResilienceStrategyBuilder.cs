// Assembly 'Microsoft.Extensions.Http.Resilience'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

public interface IHttpResilienceStrategyBuilder
{
    string StrategyName { get; }
    IServiceCollection Services { get; }
}

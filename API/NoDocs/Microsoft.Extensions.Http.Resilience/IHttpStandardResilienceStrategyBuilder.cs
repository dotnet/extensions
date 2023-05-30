// Assembly 'Microsoft.Extensions.Http.Resilience'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

public interface IHttpStandardResilienceStrategyBuilder
{
    string StrategyName { get; }
    IServiceCollection Services { get; }
}

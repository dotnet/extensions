// Assembly 'Microsoft.Extensions.Http.Resilience'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

public interface IStandardHedgingHandlerBuilder
{
    string Name { get; }
    IServiceCollection Services { get; }
    IRoutingStrategyBuilder RoutingStrategyBuilder { get; }
}

// Assembly 'Microsoft.Extensions.Http.Resilience'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

public interface IRoutingStrategyBuilder
{
    string Name { get; }
    IServiceCollection Services { get; }
}

// Assembly 'Microsoft.Extensions.Http.Resilience'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

public interface IHttpResiliencePipelineBuilder
{
    string PipelineName { get; }
    IServiceCollection Services { get; }
}

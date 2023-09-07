// Assembly 'Microsoft.Extensions.Http.Resilience'

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience;

public interface IHttpStandardResiliencePipelineBuilder
{
    string PipelineName { get; }
    IServiceCollection Services { get; }
}

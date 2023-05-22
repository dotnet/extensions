// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Resilience;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal sealed class HttpResiliencePipelineBuilder : IHttpResiliencePipelineBuilder
{
    public HttpResiliencePipelineBuilder(IResiliencePipelineBuilder<HttpResponseMessage> builder)
    {
        PipelineName = builder.PipelineName;
        Services = builder.Services;
    }

    public string PipelineName { get; }

    public IServiceCollection Services { get; }
}

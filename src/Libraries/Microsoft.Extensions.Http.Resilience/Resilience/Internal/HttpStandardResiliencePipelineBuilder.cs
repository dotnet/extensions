// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal sealed class HttpStandardResiliencePipelineBuilder : IHttpStandardResiliencePipelineBuilder
{
    public HttpStandardResiliencePipelineBuilder(IHttpResiliencePipelineBuilder builder)
    {
        PipelineName = builder.PipelineName;
        Services = builder.Services;
    }

    public string PipelineName { get; }

    public IServiceCollection Services { get; }
}

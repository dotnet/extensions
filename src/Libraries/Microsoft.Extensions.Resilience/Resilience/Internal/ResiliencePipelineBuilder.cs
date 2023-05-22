// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Resilience.Internal;

internal sealed class ResiliencePipelineBuilder<TResult> : IResiliencePipelineBuilder<TResult>
{
    internal ResiliencePipelineBuilder(IServiceCollection services, string pipelineName)
    {
        _ = Throw.IfNull(services);
        _ = Throw.IfNullOrEmpty(pipelineName);

        Services = services;
        PipelineName = pipelineName;
    }

    public string PipelineName { get; }

    public IServiceCollection Services { get; }
}

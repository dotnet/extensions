// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Resilience.Internal;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience;

internal sealed class ResiliencePipelineFactory : IResiliencePipelineFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ResiliencePipelineFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IAsyncPolicy<TResult> CreatePipeline<TResult>(string pipelineName, string pipelineKey = "")
    {
        _ = Throw.IfNullOrEmpty(pipelineName);

        // these options are automatically validated on access
        var optionsMonitor = _serviceProvider.GetRequiredService<IOptionsMonitor<ResiliencePipelineFactoryOptions<TResult>>>();
        var logger = _serviceProvider.GetRequiredService<ILogger<AsyncDynamicPipeline<TResult>>>();

        return new AsyncDynamicPipeline<TResult>(
            pipelineName,
            optionsMonitor,
            (options) =>
            {
                // The IPolicyPipelineBuilder is transient service, so we always create a new instance
                var builder = _serviceProvider.GetRequiredService<Internal.IPolicyPipelineBuilder<TResult>>();

                builder.Initialize(PipelineId.Create<TResult>(pipelineName, pipelineKey));

                foreach (var action in options.BuilderActions)
                {
                    action(builder);
                }

                return builder.Build();
            },
            logger);
    }
}

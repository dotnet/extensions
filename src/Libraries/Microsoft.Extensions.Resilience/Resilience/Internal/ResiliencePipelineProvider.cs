// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

internal sealed class ResiliencePipelineProvider : IResiliencePipelineProvider, IDisposable
{
    private readonly IResiliencePipelineFactory _factory;
    private readonly ConcurrentDictionary<(Type resultType, string pipelineName, string pipelineKey), IsPolicy> _policies = new();

    public ResiliencePipelineProvider(IResiliencePipelineFactory factory)
    {
        _factory = factory;
    }

    public IAsyncPolicy<TResult> GetPipeline<TResult>(string pipelineName)
    {
        _ = Throw.IfNullOrEmpty(pipelineName);

        // Stryker disable once all
        var key = (typeof(TResult), pipelineName, string.Empty);

        return (IAsyncPolicy<TResult>)_policies.GetOrAdd(key, static (key, factory) => factory.CreatePipeline<TResult>(key.pipelineName, string.Empty), _factory);
    }

    public IAsyncPolicy<TResult> GetPipeline<TResult>(string pipelineName, string pipelineKey)
    {
        _ = Throw.IfNullOrEmpty(pipelineName);
        _ = Throw.IfNullOrEmpty(pipelineKey);

        var key = (typeof(TResult), pipelineName, pipelineKey);

        return (IAsyncPolicy<TResult>)_policies.GetOrAdd(key, static (key, factory) => factory.CreatePipeline<TResult>(key.pipelineName, key.pipelineKey), _factory);
    }

    /// <summary>
    /// Removes all change registration subscriptions.
    /// </summary>
    public void Dispose()
    {
        foreach (var policyByKey in _policies)
        {
            if (policyByKey.Value is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _policies.Clear();
    }
}

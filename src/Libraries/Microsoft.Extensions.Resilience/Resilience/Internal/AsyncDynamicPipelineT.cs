// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Microsoft.Extensions.Resilience.Internal;

#pragma warning disable SA1649 // File name should match first type name
internal sealed class AsyncDynamicPipeline<TResult> : AsyncPolicy<TResult>, IDisposable
{
    private readonly string _pipelineName;
    private readonly Func<ResiliencePipelineFactoryOptions<TResult>, IAsyncPolicy<TResult>> _pipelineFactory;
    private readonly IDisposable? _changeListener;
    private readonly ILogger<AsyncDynamicPipeline<TResult>> _logger;

    internal IAsyncPolicy<TResult> CurrentValue { get; private set; }

    public AsyncDynamicPipeline(
        string pipelineName,
        IOptionsMonitor<ResiliencePipelineFactoryOptions<TResult>> optionsMonitor,
        Func<ResiliencePipelineFactoryOptions<TResult>, IAsyncPolicy<TResult>> factory,
        ILogger<AsyncDynamicPipeline<TResult>> logger)
    {
        _pipelineName = pipelineName;
        _pipelineFactory = factory;
        _logger = logger;

        CurrentValue = _pipelineFactory(optionsMonitor.Get(_pipelineName));
        _changeListener = optionsMonitor.OnChange(UpdatePipeline);
    }

    public void Dispose()
    {
        _changeListener?.Dispose();
    }

    protected override Task<TResult> ImplementationAsync(
        Func<Context, CancellationToken, Task<TResult>> action,
        Context context,
        CancellationToken cancellationToken,
        bool continueOnCapturedContext)
    {
        return CurrentValue.ExecuteAsync(action, context, cancellationToken, continueOnCapturedContext);
    }

    private void UpdatePipeline(
        ResiliencePipelineFactoryOptions<TResult> latestOptions,
        string? changedPipelineName)
    {
        // Stryker disable once all: Stryker tries to swap `&&` with `||`. The pipelineName cannot be null.
        if (changedPipelineName != null && changedPipelineName == _pipelineName)
        {
            try
            {
                CurrentValue = _pipelineFactory(latestOptions);
                _logger.PipelineUpdated(_pipelineName);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
            {
                // If the refresh of a pipeline fails due to any reasons, we do not want to break the entire execution,
                // hence the event will be logged and old pipeline preserved.
                _logger.PipelineUpdatedFailure(_pipelineName, ex);
            }
#pragma warning restore CA1031
        }
    }
}

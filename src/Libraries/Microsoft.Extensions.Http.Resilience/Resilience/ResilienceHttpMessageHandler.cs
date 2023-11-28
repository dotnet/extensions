// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Resilience.Internal;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// An HTTP message handler using resilience strategies to send requests.
/// </summary>
public sealed class ResilienceHttpMessageHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, ResiliencePipeline<HttpResponseMessage>> _pipelineProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceHttpMessageHandler"/> class using a resilience pipeline.
    /// </summary>
    /// <param name="pipeline">The pipeline to use for requests.</param>
    public ResilienceHttpMessageHandler(ResiliencePipeline<HttpResponseMessage> pipeline)
        : this(x => pipeline)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceHttpMessageHandler"/> class using a resilience pipeline delegate.
    /// </summary>
    /// <param name="pipelineProvider">The delegate to select a pipeline for a request.</param>
    public ResilienceHttpMessageHandler(Func<HttpRequestMessage, ResiliencePipeline<HttpResponseMessage>> pipelineProvider)
    {
        _pipelineProvider = pipelineProvider;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var pipeline = _pipelineProvider(request);
        var created = false;
        if (request.GetResilienceContext() is not ResilienceContext context)
        {
            context = ResilienceContextPool.Shared.Get(cancellationToken);
            created = true;
            request.SetResilienceContext(context);
        }

        if (request.GetRequestMetadata() is RequestMetadata requestMetadata)
        {
            context.Properties.Set(ResilienceKeys.RequestMetadata, requestMetadata);
        }

        context.Properties.Set(ResilienceKeys.RequestMessage, request);

        try
        {
            var outcome = await pipeline.ExecuteOutcomeAsync(
                static async (context, state) =>
                {
                    var request = context.Properties.GetValue(ResilienceKeys.RequestMessage, state.request);

                    // Always re-assign the context to this request message before execution.
                    // This is because for primary actions the context is also cloned and we need to re-assign it
                    // here because Polly doesn't have any other events that we can hook into.
                    request.SetResilienceContext(context);

                    try
                    {
                        var response = await state.instance.SendCoreAsync(request, context.CancellationToken).ConfigureAwait(context.ContinueOnCapturedContext);
                        return Outcome.FromResult(response);
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception e)
                    {
                        return Outcome.FromException<HttpResponseMessage>(e);
                    }
#pragma warning restore CA1031 // Do not catch general exception types
                },
                context,
                (instance: this, request))
                .ConfigureAwait(context.ContinueOnCapturedContext);

            outcome.ThrowIfException();

            return outcome.Result!;
        }
        finally
        {
            if (created)
            {
                ResilienceContextPool.Shared.Return(context);
                request.SetResilienceContext(null);
            }
            else
            {
                // Restore the original context
                request.SetResilienceContext(context);
            }
        }
    }

    private Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        => base.SendAsync(requestMessage, cancellationToken);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Base class for resilience handler, i.e. handlers that use resilience strategies to send the requests.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Resilience, UrlFormat = DiagnosticIds.UrlFormat)]
public class ResilienceHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, ResiliencePipeline<HttpResponseMessage>> _pipelineProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceHandler"/> class.
    /// </summary>
    /// <param name="pipelineProvider">The pipeline provider that supplies pipelines in response to an http message.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="pipelineProvider"/> is <see langword="null"/>.</exception>
    public ResilienceHandler(Func<HttpRequestMessage, ResiliencePipeline<HttpResponseMessage>> pipelineProvider)
    {
        _pipelineProvider = Throw.IfNull(pipelineProvider);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilienceHandler"/> class.
    /// </summary>
    /// <param name="pipeline">The pipeline to use for the message.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="pipeline"/> is <see langword="null"/>.</exception>
    public ResilienceHandler(ResiliencePipeline<HttpResponseMessage> pipeline)
    {
        _ = Throw.IfNull(pipeline);
        _pipelineProvider = _ => pipeline;
    }

    /// <summary>
    /// Sends an HTTP request to the inner handler to send to the server as an asynchronous operation.
    /// </summary>
    /// <param name="request">The HTTP request message to send to the server.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <returns>The task object representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="request"/> is <see langword="null"/>.</exception>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(request);

        ResiliencePipeline<HttpResponseMessage> pipeline = _pipelineProvider(request);

        ResilienceContext context = GetOrSetResilienceContext(request, cancellationToken, out bool created);
        TrySetRequestMetadata(context, request);
        context.SetRequestMessage(request);

        try
        {
            Outcome<HttpResponseMessage> outcome = await pipeline.ExecuteOutcomeAsync(
                static async (context, state) =>
                {
                    HttpRequestMessage request = GetRequestMessage(context, state.request);

                    // Always re-assign the context to this request message before execution.
                    // This is because for primary actions the context is also cloned and we need to re-assign it
                    // here because Polly doesn't have any other events that we can hook into.
                    request.SetResilienceContext(context);

                    try
                    {
                        HttpResponseMessage response = await state.instance
                            .SendCoreAsync(request, context.CancellationToken)
                            .ConfigureAwait(context.ContinueOnCapturedContext);

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
            RestoreResilienceContext(context, request, created);
        }
    }

#if NET6_0_OR_GREATER
    /// <summary>
    /// Sends an HTTP request to the inner handler to send to the server as a synchronous operation.
    /// </summary>
    /// <param name="request">The HTTP request message to send to the server.</param>
    /// <param name="cancellationToken">A cancellation token to cancel operation.</param>
    /// <returns>An HTTP response received from the server.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="request"/> is <see langword="null"/>.</exception>
    protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        _ = Throw.IfNull(request);

        ResiliencePipeline<HttpResponseMessage> pipeline = _pipelineProvider(request);

        ResilienceContext context = GetOrSetResilienceContext(request, cancellationToken, out bool created);
        TrySetRequestMetadata(context, request);
        context.SetRequestMessage(request);

        try
        {
            return pipeline.Execute(
                static (context, state) =>
                {
                    HttpRequestMessage request = GetRequestMessage(context, state.request);

                    // Always re-assign the context to this request message before execution.
                    // This is because for primary actions the context is also cloned and we need to re-assign it
                    // here because Polly doesn't have any other events that we can hook into.
                    request.SetResilienceContext(context);

                    return state.instance.SendCore(request, context.CancellationToken);
                },
                context,
                (instance: this, request));
        }
        finally
        {
            RestoreResilienceContext(context, request, created);
        }
    }
#endif

    private static ResilienceContext GetOrSetResilienceContext(HttpRequestMessage request, CancellationToken cancellationToken, out bool created)
    {
        created = false;

        if (request.GetResilienceContext() is not ResilienceContext context)
        {
            context = ResilienceContextPool.Shared.Get(cancellationToken);
            created = true;
            request.SetResilienceContext(context);
        }

        return context;
    }

    private static void TrySetRequestMetadata(ResilienceContext context, HttpRequestMessage request)
    {
        if (request.GetRequestMetadata() is RequestMetadata requestMetadata)
        {
            context.Properties.Set(ResilienceKeys.RequestMetadata, requestMetadata);
        }
    }

    private static HttpRequestMessage GetRequestMessage(ResilienceContext context, HttpRequestMessage request)
        => context.GetRequestMessage() ?? request;

    private static void RestoreResilienceContext(ResilienceContext context, HttpRequestMessage request, bool created)
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

    private Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        => base.SendAsync(requestMessage, cancellationToken);

#if NET6_0_OR_GREATER
    private HttpResponseMessage SendCore(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        => base.Send(requestMessage, cancellationToken);
#endif
}

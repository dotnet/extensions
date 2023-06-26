﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Telemetry;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// Base class for resilience handler, i.e. handlers that use resilience strategies  to send the requests.
/// </summary>
internal sealed class ResilienceHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, ResilienceStrategy<HttpResponseMessage>> _strategyProvider;

    public ResilienceHandler(Func<HttpRequestMessage, ResilienceStrategy<HttpResponseMessage>> strategyProvider)
    {
        _strategyProvider = strategyProvider;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var strategy = _strategyProvider(request);
        var created = false;
        if (request.GetResilienceContext() is not ResilienceContext context)
        {
            context = ResilienceContext.Get();
            created = true;
            request.SetResilienceContext(context);
        }

        if (request.GetRequestMetadata() is RequestMetadata requestMetadata)
        {
            context.Properties.Set(ResilienceKeys.RequestMetadata, requestMetadata);
        }

        context.Properties.Set(ResilienceKeys.RequestMessage, request);
        var previousToken = context.CancellationToken;
        context.CancellationToken = cancellationToken;

        try
        {
            var outcome = await strategy.ExecuteOutcomeAsync(
                static async (context, state) =>
                {
                    var request = context.Properties.GetValue(ResilienceKeys.RequestMessage, state.request);

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

            outcome.EnsureSuccess();

            return outcome.Result!;
        }
        finally
        {
            if (created)
            {
                ResilienceContext.Return(context);
                request.SetResilienceContext(null);
            }
            else
            {
                context.CancellationToken = previousToken;
            }
        }
    }

    private Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        => base.SendAsync(requestMessage, cancellationToken);
}

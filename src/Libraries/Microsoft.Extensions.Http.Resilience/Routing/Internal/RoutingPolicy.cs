// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.Routing.Internal;

/// <summary>
/// Adds routing support to an inner policy.
/// </summary>
internal sealed class RoutingPolicy : AsyncPolicy<HttpResponseMessage>
{
    private readonly IRequestRoutingStrategyFactory _factory;
    private readonly Func<Context, HttpRequestMessage?> _requestProvider;
    private readonly Action<Context, IRequestRoutingStrategy> _routingStrategySetter;

    public RoutingPolicy(string pipelineName, IRequestRoutingStrategyFactory factory)
    {
        _factory = factory;
        _requestProvider = ContextExtensions.CreateRequestMessageProvider(pipelineName);
        _routingStrategySetter = HedgingContextExtensions.CreateRoutingStrategySetter(pipelineName);
    }

    protected override async Task<HttpResponseMessage> ImplementationAsync(
        Func<Context, CancellationToken, Task<HttpResponseMessage>> action,
        Context context,
        CancellationToken cancellationToken,
        bool continueOnCapturedContext)
    {
        var strategy = _factory.CreateRoutingStrategy();

        // if there are not routes we cannot continue
        if (!strategy.TryGetNextRoute(out var route))
        {
            Throw.InvalidOperationException("The routing strategy did not provide any route URL on the first attempt.");
        }

        _routingStrategySetter(context, strategy);

        var request = _requestProvider(context)!;

        // for primary request, use retrieved route
        request.RequestUri = request.RequestUri!.ReplaceHost(route!);

        try
        {
            return await action(context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (_factory is IPooledRequestRoutingStrategyFactory pooled)
            {
                pooled.ReturnRoutingStrategy(strategy);
            }
        }
    }
}

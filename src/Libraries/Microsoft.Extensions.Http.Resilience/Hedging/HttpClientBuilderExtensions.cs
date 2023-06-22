﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.Resilience.Internal;
using Microsoft.Extensions.Resilience;

namespace Microsoft.Extensions.Http.Resilience;

public static partial class HttpClientBuilderExtensions
{
    internal static HedgedTaskProvider<HttpResponseMessage> CreateHedgedTaskProvider(string pipelineName)
    {
        var invokerProvider = Internal.ContextExtensions.CreateMessageInvokerProvider(pipelineName);
        var routingStrategyProvider = HedgingContextExtensions.CreateRoutingStrategyProvider(pipelineName);
        var snapshotProvider = HedgingContextExtensions.CreateRequestMessageSnapshotProvider(pipelineName);

        return (HedgingTaskProviderArguments args, out Task<HttpResponseMessage>? result) =>
        {
            // retrieve active routing strategy that was attached by RoutingPolicy
            var strategy = routingStrategyProvider(args.Context)!;
            if (!strategy.TryGetNextRoute(out var route))
            {
                result = null;

                // Stryker disable once Boolean
                return false;
            }

            var snapshot = snapshotProvider(args.Context)!;
            var request = snapshot.Create();
            request.RequestUri = request.RequestUri!.ReplaceHost(route);
            var invoker = invokerProvider(args.Context)!;

            // send cloned request to a inner delegating handler
            result = invoker.SendAsync(request, args.CancellationToken);

            return true;
        };
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Shared.Diagnostics;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class HedgingContextExtensions
{
    private const string RoutingStrategyKey = "Hedging.RoutingStrategy";
    private const string SnapshotKey = "Hedging.RequestMessageSnapshot";

    internal static Func<Context, IHttpRequestMessageSnapshot?> CreateRequestMessageSnapshotProvider(string pipelineName)
    {
        _ = Throw.IfNullOrEmpty(pipelineName);

        var key = $"{SnapshotKey}-{pipelineName}";

        return (context) =>
        {
            if (context.TryGetValue(key, out var val))
            {
                return (IHttpRequestMessageSnapshot)val;
            }

            return null;
        };
    }

    internal static Action<Context, IHttpRequestMessageSnapshot> CreateRequestMessageSnapshotSetter(string pipelineName)
    {
        var key = $"{SnapshotKey}-{pipelineName}";

        return (context, snapshot) => context[key] = snapshot;
    }

    internal static Func<Context, IRequestRoutingStrategy?> CreateRoutingStrategyProvider(string pipelineName)
    {
        _ = Throw.IfNullOrEmpty(pipelineName);

        var key = $"{RoutingStrategyKey}-{pipelineName}";

        return (context) =>
        {
            if (context.TryGetValue(key, out var val))
            {
                return (IRequestRoutingStrategy)val;
            }

            return null;
        };
    }

    internal static Action<Context, IRequestRoutingStrategy> CreateRoutingStrategySetter(string pipelineName)
    {
        var key = $"{RoutingStrategyKey}-{pipelineName}";

        return (context, invoker) => context[key] = invoker;
    }
}

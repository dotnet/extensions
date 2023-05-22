// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Http.Resilience.Internal.Routing;

internal static class RoutingHelper
{
    public static T SelectByWeight<T>(this IList<T> endpoints, Func<T, double> weightProvider, IRandomizer randomizer)
    {
        var accumulatedProbability = 0d;
        var weightSum = 0d;

        for (int i = 0; i < endpoints.Count; i++)
        {
            weightSum += weightProvider(endpoints[i]);
        }

        var randomPercentageValue = randomizer.NextDouble(weightSum);
        for (int i = 0; i < endpoints.Count; i++)
        {
            var endpoint = endpoints[i];
            var weight = weightProvider(endpoint);

            if (randomPercentageValue <= weight + accumulatedProbability)
            {
                return endpoint;
            }

            accumulatedProbability += weight;
        }

        throw new InvalidOperationException($"The item cannot be selected because the weights are not correctly calculated.");
    }
}

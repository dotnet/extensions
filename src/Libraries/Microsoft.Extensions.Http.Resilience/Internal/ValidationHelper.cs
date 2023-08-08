// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Polly;
using Polly.Retry;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class ValidationHelper
{
    public static TimeSpan GetAggregatedDelay<T>(RetryStrategyOptions<T> options)
    {
        // Instead of re-implementing the calculations of delays we can just
        // execute the retry strategy and aggregate the delays by using the RetryDelayGenerator
        // callback that receives the delay hint for each attempt.

        try
        {
            var aggregatedDelay = TimeSpan.Zero;
            var builder = new CompositeStrategyBuilder
            {
                Randomizer = () => 1.0 // disable randomization so the output is always the same
            };

            builder.AddRetry(new()
            {
                RetryCount = options.RetryCount,
                BaseDelay = options.BaseDelay,
                BackoffType = options.BackoffType,
                ShouldHandle = _ => PredicateResult.True, // always retry until all retries are exhausted
                RetryDelayGenerator = args =>
                {
                    // the delay hint is calculated for this attempt by the retry strategy
                    aggregatedDelay += args.Arguments.DelayHint;

                    // return zero delay, so no waiting
                    return new ValueTask<TimeSpan>(TimeSpan.Zero);
                },
            })
            .Build()
            .Execute(static () => { }); // this executes all retries and we aggregate the delays immediately

            return aggregatedDelay;
        }
        catch (OverflowException)
        {
            return TimeSpan.MaxValue;
        }
    }
}

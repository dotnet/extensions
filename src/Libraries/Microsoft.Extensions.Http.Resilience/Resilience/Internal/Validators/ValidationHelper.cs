// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.Extensions.Resilience;
using Microsoft.Extensions.Resilience.Options;

namespace Microsoft.Extensions.Http.Resilience.Internal.Validators;

internal static class ValidationHelper
{
    public static TimeSpan GetRetryPolicyDelaySum(this RetryPolicyOptions retryPolicyOptions)
    {
        return retryPolicyOptions.BackoffType == BackoffType.ExponentialWithJitter ?
            retryPolicyOptions.GetExponentialWithJitterDeterministicDelay() :
            retryPolicyOptions.GetDelays().Aggregate((accumulated, current) => accumulated + current);
    }

    /// <summary>
    /// Calculates the upper-bound cumulated delay of the given retry policy options by using the algorithm defined in
    /// https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry/blob/master/src/Polly.Contrib.WaitAndRetry/Backoff.DecorrelatedJitterV2.cs,
    /// with the randomized jitter factor (a value between 0 and 1) replaced with 1.
    /// </summary>
    /// <param name="options">The retry policy options.</param>
    /// <returns>The calculated upper-bound cumulated delay of the retry policy.</returns>
    public static TimeSpan GetExponentialWithJitterDeterministicDelay(this RetryPolicyOptions options)
    {
        var totalDelay = TimeSpan.Zero;

        const double Factor = 4.0;
        const double ScalingFactor = 1 / 1.4d;
        var maxTimeSpanDouble = TimeSpan.MaxValue.Ticks - 1000;
        var targetTicksFirstDelay = options.BaseDelay.Ticks;

        var prev = 0.0;
        for (int i = 0; EvaluateRetry(i, options.RetryCount); i++)
        {
            var t = i + 1.0;
            var next = Math.Pow(2, t) * Math.Tanh(Math.Sqrt(Factor * t));

            var formulaIntrinsicValue = next - prev;
            var diff = (long)Math.Min(formulaIntrinsicValue * ScalingFactor * targetTicksFirstDelay, maxTimeSpanDouble);

            try
            {
                totalDelay += TimeSpan.FromTicks(diff);
            }
            catch (OverflowException)
            {
                return TimeSpan.FromTicks(maxTimeSpanDouble);
            }

            prev = next;
        }

        return totalDelay;
    }

    private static bool EvaluateRetry(int retry, int maxRetryCount) => retry < maxRetryCount || maxRetryCount == RetryPolicyOptions.InfiniteRetry;
}

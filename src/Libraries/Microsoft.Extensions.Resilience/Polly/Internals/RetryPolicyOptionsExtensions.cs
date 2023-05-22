// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Resilience.Options;
using Microsoft.Shared.Diagnostics;
using Polly.Contrib.WaitAndRetry;

namespace Microsoft.Extensions.Resilience;

/// <summary>
/// Extensions for <see cref="RetryPolicyOptions"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RetryPolicyOptionsExtensions
{
    /// <summary>
    /// Gets the delays generated based on the retry options configuration.
    /// </summary>
    /// <param name="options">The options instance.</param>
    /// <returns>The delays collection.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static IEnumerable<TimeSpan> GetDelays(this RetryPolicyOptions options)
    {
        _ = Throw.IfNull(options);

        var delays = GetDelayByBackoffType(options.BackoffType, options.BaseDelay, options.RetryCount);

        if (options.BackoffType == BackoffType.ExponentialWithJitter)
        {
            // We cannot materialize ExponentialWithJitter delays as every iteration returns slightly different delays.
            // Materializing and caching it would remove the randomness factor in retry policy.
            return delays;
        }

        // here, the delays are the same so we can materialize the list
        return delays.ToList();
    }

    internal static IEnumerable<TimeSpan> GetDelayByBackoffType(BackoffType retryType, TimeSpan backoffBasedDelay, int retryCount)
    {
        return retryType switch
        {
            BackoffType.ExponentialWithJitter => Backoff.DecorrelatedJitterBackoffV2(backoffBasedDelay, retryCount),
            BackoffType.Linear => Backoff.LinearBackoff(backoffBasedDelay, retryCount),
            BackoffType.Constant => Backoff.ConstantBackoff(backoffBasedDelay, retryCount),
            _ => throw new InvalidOperationException($"{backoffBasedDelay} back-off type is not supported.")
        };
    }
}

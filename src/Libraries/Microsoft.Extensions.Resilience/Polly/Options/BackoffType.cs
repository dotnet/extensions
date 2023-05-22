// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.Options;

/// <summary>
/// Retry type.
/// </summary>
public enum BackoffType
{
    /// <summary>
    /// Exponential delay with randomization retry type,
    /// making sure to mitigate any correlations.
    /// </summary>
    /// <example>
    /// 850ms, 1455ms, 3060ms.
    /// </example>
    /// <remarks>
    /// In transient failures handling scenarios, this is the
    /// <see href=" https://github.com/Polly-Contrib/Polly.Contrib.WaitAndRetry#new-jitter-recommendation"> recommended retry type</see>.
    /// </remarks>
    ExponentialWithJitter,

    /// <summary>
    /// The constant retry type.
    /// </summary>
    /// <example>
    /// 200ms, 200ms, 200ms, etc.
    /// </example>
    /// <remarks>
    /// It ensures a constant wait duration before each retry attempt.
    /// For concurrent database access with possibility of conflicting updates,
    /// retrying the failures in a constant manner allows consistent transient failures mitigation.
    /// </remarks>
    Constant,

    /// <summary>
    /// The linear retry type.
    /// </summary>
    /// <example>
    /// 100ms, 200ms, 300ms, 400ms, etc.
    /// </example>
    /// <remarks>
    /// Generates sleep durations in an linear manner.
    /// In case randomization introduced by the jitter and exponential growth are not intended,
    /// the linear growth allows more control over the delay intervals.
    /// </remarks>
    Linear
}

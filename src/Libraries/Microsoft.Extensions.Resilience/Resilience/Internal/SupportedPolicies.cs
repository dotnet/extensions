// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.Internal;

/// <summary>
/// Represents the policies supported by R9.
/// </summary>
internal enum SupportedPolicies
{
    /// <summary>
    /// The circuit breaker policy type.
    /// </summary>
    CircuitBreaker = 0,

    /// <summary>
    /// The retry policy type.
    /// </summary>
    RetryPolicy = 1,

    /// <summary>
    /// The timeout policy type.
    /// </summary>
    TimeoutPolicy = 2,

    /// <summary>
    /// The fallback policy type.
    /// </summary>
    FallbackPolicy = 3,

    /// <summary>
    /// The bulkhead policy type.
    /// </summary>
    BulkheadPolicy = 4,

    /// <summary>
    /// The hedging policy type.
    /// </summary>
    HedgingPolicy = 5
}

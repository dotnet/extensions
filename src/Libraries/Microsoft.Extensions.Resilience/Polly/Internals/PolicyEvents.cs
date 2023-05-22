// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.Internal;
internal static class PolicyEvents
{
    public const string FallbackPolicyEvent = "FallbackPolicy-OnFallback";
    public const string CircuitBreakerOnBreakPolicyEvent = "CircuitBreakerPolicy-OnBreak";
    public const string CircuitBreakerOnResetPolicyEvent = "CircuitBreakerPolicy-OnReset";
    public const string CircuitBreakerOnHalfOpenPolicyEvent = "CircuitBreakerPolicy-OnHalfOpen";
    public const string RetryPolicyEvent = "RetryPolicy-OnRetry";
    public const string TimeoutPolicyEvent = "TimeoutPolicy-OnTimeout";
    public const string BulkheadPolicyEvent = "BulkheadPolicy-OnBulkheadRejected";
    public const string HedgingPolicyEvent = "HedgingPolicy-OnHedgingAsync";
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Resilience.Options;

namespace Microsoft.Extensions.Resilience.Polly.Test.Options;

public static class Constants
{
    public static class BulkheadPolicy
    {
        public static readonly BulkheadPolicyOptions DefaultOptions = new();
    }

    public static class CircuitBreakerPolicy
    {
        public static CircuitBreakerPolicyOptions<TResult> DefaultOptions<TResult>() => new();
    }

    public static class FallbackPolicy
    {
        public static FallbackPolicyOptions<TResult> DefaultOptions<TResult>() => new();
    }

    public static class HedgingPolicy
    {
        public static HedgingPolicyOptions<TResult> DefaultOptions<TResult>() => new();
    }

    public static class HedgingPolicyNonGeneric
    {
        public static HedgingPolicyOptions DefaultOptions() => new();
    }

    public static class RetryPolicy
    {
        public static RetryPolicyOptions<TResult> DefaultOptions<TResult>() => new();
    }

    public static class RetryPolicyNonGeneric
    {
        public static RetryPolicyOptions DefaultOptions() => new();
    }

    public static class TimeoutPolicy
    {
        public static TimeoutPolicyOptions DefaultOptions => new();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class StandardStrategyNames
{
    public const string CircuitBreaker = "Standard-CircuitBreaker";

    public const string RateLimiter = "Standard-RateLimiter";

    public const string Retry = "Standard-Retry";

    public const string TotalRequestTimeout = "Standard-TotalRequestTimeout";

    public const string AttemptTimeout = "Standard-AttemptTimeout";
}

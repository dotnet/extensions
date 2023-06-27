// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Resilience.Internal.Validators;

internal static class StandardHedgingStrategyNames
{
    public const string CircuitBreaker = "StandardHedging-CircuitBreaker";

    public const string RateLimiter = "StandardHedging-RateLimiter";

    public const string Hedging = "StandardHedging-Hedging";

    public const string TotalRequestTimeout = "StandardHedging-TotalRequestTimeout";

    public const string AttemptTimeout = "StandardHedging-AttemptTimeout";
}

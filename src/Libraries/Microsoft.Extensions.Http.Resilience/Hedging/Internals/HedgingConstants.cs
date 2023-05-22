// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class HedgingConstants
{
    public const string DeprecatedMessage = "Deprecated since 1.23.0 and will be removed in 1.32.0. " +
        "Use standard hedging instead. If something prevents you from switching to standard hedging contact R9 team with your scenario " +
        "and we can either extend the standard hedging or postpone the deletion of this API and making it official.";
}

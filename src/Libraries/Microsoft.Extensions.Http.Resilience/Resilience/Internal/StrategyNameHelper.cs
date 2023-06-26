// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class StrategyNameHelper
{
    public static string GetName(string httpClientName, string strategyIdentifier) => $"{httpClientName}-{strategyIdentifier}";
}

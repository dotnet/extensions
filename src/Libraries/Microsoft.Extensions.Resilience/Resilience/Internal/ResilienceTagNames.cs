// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.Internal;

internal static class ResilienceTagNames
{
    public const string FailureSource = "resilience.failure.source";

    public const string FailureReason = "resilience.failure.reason";

    public const string FailureSummary = "resilience.failure.summary";

    public const string DependencyName = "resilience.dependency.name";

    public const string RequestName = "resilience.request.name";
}

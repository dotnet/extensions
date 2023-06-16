// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.Resilience.Internal;

internal static class ResilienceDimensions
{
    public const string FailureSource = "failure-source";

    public const string FailureReason = "failure-reason";

    public const string FailureSummary = "failure-summary";

    public const string DependencyName = "dep-name";

    public const string RequestName = "req-name";
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.Internal;

internal static class ResilienceTagNames
{
    public const string FailureSource = "dotnet.resilience.failure.source";

    public const string FailureReason = "dotnet.resilience.failure.reason";

    public const string FailureSummary = "dotnet.resilience.failure.summary";

    public const string DependencyName = "dotnet.resilience.dependency.name";

    public const string RequestName = "dotnet.resilience.request.name";
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1716
namespace Microsoft.Shared.DiagnosticIds;
#pragma warning restore CA1716

/// <summary>
/// Experiments supported by this repo.
/// </summary>
/// <remarks>
/// When adding a new experiment, add a corresponding suppression to the root <c>Directory.Build.targets</c> file, and add a documentation entry to
/// <c>docs/list-of-diagnostics.md</c>.
/// </remarks>
internal static class Experiments
{
#pragma warning disable S1075 // URIs should not be hardcoded
    internal const string UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}";
#pragma warning restore S1075 // URIs should not be hardcoded

    internal const string Resilience = "EXTEXP0001";
    internal const string Compliance = "EXTEXP0002";
    internal const string Telemetry = "EXTEXP0003";
    internal const string TimeProvider = "EXTEXP0004";
    internal const string AutoClient = "EXTEXP0005";
    internal const string AsyncState = "EXTEXP0006";
    internal const string HealthChecks = "EXTEXP0007";
    internal const string ResourceMonitoring = "EXTEXP0008";
    internal const string Hosting = "EXTEXP0009";
    internal const string ObjectPool = "EXTEXP0010";
    internal const string DocumentDb = "EXTEXP0011";
    internal const string AutoActivation = "EXTEXP0012";
}

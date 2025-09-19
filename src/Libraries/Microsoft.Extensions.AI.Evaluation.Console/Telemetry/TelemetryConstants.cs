// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Microsoft.Extensions.AI.Evaluation.Console.Telemetry;

internal static class TelemetryConstants
{
    internal const string ConnectionString =
        "InstrumentationKey=00000000-0000-0000-0000-000000000000;" +
        "IngestionEndpoint=https://eastus-8.in.applicationinsights.azure.com/;" +
        "LiveEndpoint=https://eastus.livediagnostics.monitor.azure.com/;" +
        "ApplicationId=00000000-0000-0000-0000-000000000000";

    internal const string EventNamespace = "dotnet/aieval";

    internal static class EventNames
    {
        internal const string CleanCacheCommand = "CleanCacheCommand";
        internal const string CleanResultsCommand = "CleanResultsCommand";
        internal const string ReportCommand = "ReportCommand";

        internal const string ScenarioRun = "ScenarioRun";
        internal const string ModelUsageDetails = "ModelUsageDetails";
    }

    internal static class PropertyNames
    {
        // Properties common to all events.
        internal const string DevDeviceId = "DevDeviceId";
        internal const string OSVersion = "OSVersion";
        internal const string OSPlatform = "OSPlatform";
        internal const string KernelVersion = "KernelVersion";
        internal const string RuntimeId = "RuntimeId";
        internal const string ProductVersion = "ProductVersion";
        internal const string IsCIEnvironment = "IsCIEnvironment";

        // Properties common to all *Command events.
        internal const string Success = "Success";
        internal const string DurationInMilliseconds = "DurationInMilliseconds";

        // Properties for parameters included in corresponding *Command events.
        internal const string StorageType = "StorageType";
        internal const string LastN = "LastN";
        internal const string Format = "Format";
        internal const string OpenReport = "OpenReport";

        // Properties included in the ReportCommand event.
        internal const string TotalScenarioRunCount = "TotalScenarioRunCount";
        internal const string TurnCount = "TurnCount";
        internal const string TotalTokenCount = "TotalTokenCount";
        internal const string InputTokenCount = "InputTokenCount";
        internal const string WellKnownHostTurnCount = "WellKnownHostTurnCount";
        internal const string WellKnownHostTotalTokenCount = "WellKnownHostTotalTokenCount";
        internal const string WellKnownHostInputTokenCount = "WellKnownHostInputTokenCount";
        internal const string LocalMachineHostTurnCount = "LocalMachineHostTurnCount";
        internal const string LocalMachineHostTotalTokenCount = "LocalMachineHostTotalTokenCount";
        internal const string LocalMachineHostInputTokenCount = "LocalMachineHostInputTokenCount";

        // Properties included in the ScenarioRun event.
        internal const string TotalMetricsCount = "TotalMetricsCount";
        internal const string BuiltInMetricsUsed = "BuiltInMetricsUsed";
        internal const string BuiltInMetricsCount = "BuiltInMetricsCount";
        internal const string FailingMetricsCount = "FailingMetricsCount";
        internal const string TotalDiagnosticsCount = "TotalDiagnosticsCount";
        internal const string ErrorDiagnosticsCount = "ErrorDiagnosticsCount";
        internal const string WarningDiagnosticsCount = "WarningDiagnosticsCount";

        // Properties included in the ModelUsageDetails event.
        internal const string Model = "Model";
        internal const string ModelProvider = "ModelProvider";
        internal const string IsModelHostWellKnown = "IsModelHostWellKnown";
        internal const string IsModelHostedLocally = "IsModelHostedLocally";
    }

    internal static class PropertyValues
    {
        internal const string Unknown = "unknown";

        internal const string True = "true";
        internal const string False = "false";

        internal const string StorageTypeDisk = "disk";
        internal const string StorageTypeAzure = "azure";
    }

#pragma warning disable S103 // Lines should not be too long
    internal const string TelemetryOptOutMessage =
        $"""
        ---------
        Telemetry
        ---------
        The aieval .NET tool collects usage data in order to help us improve your experience. The data is anonymous and doesn't include personal information. You can opt-out of this data collection by setting the {TelemetryConstants.TelemetryOptOutEnvironmentVariableName} environment variable to '1' or 'true' using your favorite shell.
        """;
#pragma warning restore S103

    internal const string TelemetryOptOutEnvironmentVariableName = "DOTNET_AIEVAL_TELEMETRY_OPTOUT";
    private const string SkipFirstTimeExperienceEnvironmentVariableName = "DOTNET_AIEVAL_SKIP_FIRST_TIME_EXPERIENCE";

    internal static bool IsTelemetryEnabled { get; } =
        !EnvironmentHelper.GetEnvironmentVariableAsBool(TelemetryOptOutEnvironmentVariableName) &&
        (!IsFirstTimeExperienceEnabled || File.Exists(FirstUseSentinelFilePath));

    internal static bool IsFirstTimeExperienceEnabled { get; } =
        !EnvironmentHelper.GetEnvironmentVariableAsBool(SkipFirstTimeExperienceEnvironmentVariableName);

    internal static string? FirstUseSentinelFilePath { get; } = GetFirstUseSentinelFilePath();

    private static string? GetFirstUseSentinelFilePath()
    {
        string? homeDirectoryPath = Environment.GetEnvironmentVariable("DOTNET_CLI_HOME");
        if (string.IsNullOrWhiteSpace(homeDirectoryPath))
        {
            homeDirectoryPath =
                Environment.GetEnvironmentVariable(
                    RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                        ? "USERPROFILE"
                        : "HOME");
        }

        string? sentinelFilePath =
            string.IsNullOrWhiteSpace(homeDirectoryPath)
                ? null
                : Path.Combine(homeDirectoryPath, ".dotnet", $"{Constants.Version}.aieval.dotnetFirstUseSentinel");

        return sentinelFilePath;
    }
}

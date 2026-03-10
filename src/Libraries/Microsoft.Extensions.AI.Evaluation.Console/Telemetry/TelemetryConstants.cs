// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI.Evaluation.Console.Telemetry;

internal static class TelemetryConstants
{
    internal const string ConnectionString = "InstrumentationKey=469489a6-628b-4bb9-80db-ec670f70d874";

    internal const string EventNamespace = "dotnet/aieval";

    internal static class EventNames
    {
        internal const string CleanCacheCommand = "CleanCacheCommand";
        internal const string CleanResultsCommand = "CleanResultsCommand";
        internal const string ReportCommand = "ReportCommand";

        internal const string ScenarioRunResult = "ScenarioRunResult";
        internal const string BuiltInMetric = "BuiltInMetric";
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

        // Properties included in the ScenarioRunResult event.
        internal const string ScenarioRunResultId = "ScenarioRunResultId";
        internal const string MetricsCount = "MetricsCount";

        // Properties included in the BuiltInMetric event.
        internal const string MetricName = "MetricName";
        internal const string InputTokenCount = "InputTokenCount";
        internal const string OutputTokenCount = "OutputTokenCount";
        internal const string IsInterpretedAsFailed = "IsInterpretedAsFailed";
        internal const string ErrorDiagnosticsCount = "ErrorDiagnosticsCount";
        internal const string WarningDiagnosticsCount = "WarningDiagnosticsCount";
        internal const string InformationalDiagnosticsCount = "InformationalDiagnosticsCount";

        // Properties included in the ModelUsageDetails event.
        internal const string Model = "Model";
        internal const string ModelProvider = "ModelProvider";
        internal const string IsModelHostWellKnown = "IsModelHostWellKnown";
        internal const string IsModelHostedLocally = "IsModelHostedLocally";
        internal const string CachedTurnCount = "CachedTurnCount";
        internal const string NonCachedTurnCount = "NonCachedTurnCount";
        internal const string CachedInputTokenCount = "CachedInputTokenCount";
        internal const string NonCachedInputTokenCount = "NonCachedInputTokenCount";
        internal const string CachedOutputTokenCount = "CachedOutputTokenCount";
        internal const string NonCachedOutputTokenCount = "NonCachedOutputTokenCount";
    }

    internal static class PropertyValues
    {
        internal const string Unknown = "unknown";

        internal const string StorageTypeDisk = "disk";
        internal const string StorageTypeAzure = "azure";

        // Kusto recongnizes "true" and "false" as boolean literals.
        internal const string True = "true";
        internal const string False = "false";
    }

    private const string TelemetryOptOutEnvironmentVariableName = "DOTNET_AIEVAL_TELEMETRY_OPTOUT";
    private const string SkipFirstTimeExperienceEnvironmentVariableName = "DOTNET_AIEVAL_SKIP_FIRST_TIME_EXPERIENCE";
    private const string TelemetryOptOutMessage =
        $"""
        ---------
        Telemetry
        ---------
        The aieval .NET tool collects usage data in order to help us improve your experience. The data is anonymous and doesn't include personal information. You can opt-out of this data collection by setting the {TelemetryOptOutEnvironmentVariableName} environment variable to '1' or 'true' using your favorite shell.
        """;

    private static readonly string? _firstUseSentinelFilePath;
    private static readonly bool _firstUseSentinelFileExists;
    private static readonly bool _shouldDisplayTelemetryOptOutMessage;

    internal static bool IsTelemetryEnabled { get; }

#pragma warning disable CA1810
    // CA1810: Initialize all static fields when declared.
    // We disable this warning because the static fields above must be initialized in a specific order which is not
    // guaranteed when initializing via field initializers.
    static TelemetryConstants()
#pragma warning restore CA1810
    {
        _firstUseSentinelFilePath = GetFirstUseSentinelFilePath();
        _firstUseSentinelFileExists = File.Exists(_firstUseSentinelFilePath);

        _shouldDisplayTelemetryOptOutMessage =
            !EnvironmentHelper.GetEnvironmentVariableAsBoolean(SkipFirstTimeExperienceEnvironmentVariableName) &&
            !_firstUseSentinelFileExists;

        IsTelemetryEnabled =
            !EnvironmentHelper.GetEnvironmentVariableAsBoolean(TelemetryOptOutEnvironmentVariableName) &&
            !_shouldDisplayTelemetryOptOutMessage;

        static string? GetFirstUseSentinelFilePath()
        {
            string? homeDirectoryPath = Environment.GetEnvironmentVariable("DOTNET_CLI_HOME");
            if (string.IsNullOrWhiteSpace(homeDirectoryPath))
            {
                homeDirectoryPath =
                    Environment.GetEnvironmentVariable(
                        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "USERPROFILE" : "HOME");
            }

            string? sentinelFilePath =
                string.IsNullOrWhiteSpace(homeDirectoryPath)
                    ? null
                    : Path.Combine(homeDirectoryPath, ".dotnet", $"{Constants.Version}.aieval.dotnetFirstUseSentinel");

            return sentinelFilePath;
        }
    }

#pragma warning disable EA0014 // The async method doesn't support cancellation.
    internal static async Task DisplayTelemetryOptOutMessageIfNeededAsync(this ILogger logger)
#pragma warning restore EA0014
    {
        if (_shouldDisplayTelemetryOptOutMessage)
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters.
            // Use Console.WriteLine directly instead of ILogger to ensure proper formatting.
            System.Console.WriteLine(TelemetryOptOutMessage);
            System.Console.WriteLine();
#pragma warning restore CA1303
        }

        if (_firstUseSentinelFilePath is null)
        {
            logger.LogWarning("Could not determine sentinel file path.");
            return;
        }

        if (_firstUseSentinelFileExists)
        {
            return;
        }

        try
        {
            await File.WriteAllBytesAsync(_firstUseSentinelFilePath, []).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create sentinel file.");
        }
    }
}

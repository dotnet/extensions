// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Microsoft.Extensions.AI.Evaluation.Console.Telemetry;
using Microsoft.Extensions.AI.Evaluation.Console.Utilities;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Html;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Json;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.AI.Evaluation.Utilities;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI.Evaluation.Console.Commands;

internal sealed partial class ReportCommand(ILogger logger, TelemetryHelper telemetryHelper)
{
    internal async Task<int> InvokeAsync(
        DirectoryInfo? storageRootDir,
        Uri? endpointUri,
        FileInfo outputFile,
        bool openReport,
        int lastN,
        Format format,
        CancellationToken cancellationToken = default)
    {
        var telemetryProperties =
            new Dictionary<string, string>
            {
                [TelemetryConstants.PropertyNames.LastN] = lastN.ToTelemetryPropertyValue(),
                [TelemetryConstants.PropertyNames.Format] = format.ToString(),
                [TelemetryConstants.PropertyNames.OpenReport] = openReport.ToTelemetryPropertyValue()
            };

        await logger.ExecuteWithCatchAsync(
            operation: () =>
                telemetryHelper.ReportOperationAsync(
                    operationName: TelemetryConstants.EventNames.ReportCommand,
                    operation: async ValueTask () =>
                    {
                        IEvaluationResultStore resultStore;

                        if (storageRootDir is not null)
                        {
                            string storageRootPath = storageRootDir.FullName;
                            logger.LogInformation("Storage root path: {storageRootPath}", storageRootPath);

                            resultStore = new DiskBasedResultStore(storageRootPath);

                            telemetryProperties[TelemetryConstants.PropertyNames.StorageType] =
                                TelemetryConstants.PropertyValues.StorageTypeDisk;
                        }
                        else if (endpointUri is not null)
                        {
                            logger.LogInformation("Azure Storage endpoint: {endpointUri}", endpointUri);

                            var fsClient = new DataLakeDirectoryClient(endpointUri, new DefaultAzureCredential());
                            resultStore = new AzureStorageResultStore(fsClient);

                            telemetryProperties[TelemetryConstants.PropertyNames.StorageType] =
                                TelemetryConstants.PropertyValues.StorageTypeAzure;
                        }
                        else
                        {
                            throw new InvalidOperationException("Either --path or --endpoint must be specified");
                        }

                        List<ScenarioRunResult> results = [];
                        string? latestExecutionName = null;
                        long totalScenarioRunCount = 0;
                        var overallUsageDetails = new UsageDetailsWithTurnCount();
                        var wellKnownHostUsageDetails = new UsageDetailsWithTurnCount();
                        var localMachineHostUsageDetails = new UsageDetailsWithTurnCount();
                        var usageDetailsByModel =
                            new Dictionary<(string? model, string? modelProvider), UsageDetailsWithTurnCount>();

                        await foreach (string executionName in
                            resultStore.GetLatestExecutionNamesAsync(lastN, cancellationToken).ConfigureAwait(false))
                        {
                            latestExecutionName ??= executionName;

                            await foreach (ScenarioRunResult result in
                                resultStore.ReadResultsAsync(
                                    executionName,
                                    cancellationToken: cancellationToken).ConfigureAwait(false))
                            {
                                if (result.ExecutionName == latestExecutionName)
                                {
                                    ++totalScenarioRunCount;

                                    ReportScenarioRun(
                                        result,
                                        overallUsageDetails,
                                        wellKnownHostUsageDetails,
                                        localMachineHostUsageDetails,
                                        usageDetailsByModel);
                                }
                                else
                                {
                                    // Clear the chat data for following executions
                                    result.Messages = [];
                                    result.ModelResponse = new ChatResponse();
                                }

                                results.Add(result);

                                logger.LogInformation(
                                    "Execution: {executionName} Scenario: {scenarioName} Iteration: {iterationName}",
                                    result.ExecutionName,
                                    result.ScenarioName,
                                    result.IterationName);
                            }
                        }

                        telemetryProperties[TelemetryConstants.PropertyNames.TotalScenarioRunCount] =
                            totalScenarioRunCount.ToTelemetryPropertyValue();

                        ReportUsageDetails(
                            telemetryProperties,
                            overallUsageDetails,
                            wellKnownHostUsageDetails,
                            localMachineHostUsageDetails,
                            usageDetailsByModel);

                        string outputFilePath = outputFile.FullName;
                        string? outputPath = Path.GetDirectoryName(outputFilePath);
                        if (outputPath is not null && !Directory.Exists(outputPath))
                        {
                            _ = Directory.CreateDirectory(outputPath);
                        }

                        IEvaluationReportWriter reportWriter = format switch
                        {
                            Format.html => new HtmlReportWriter(outputFilePath),
                            Format.json => new JsonReportWriter(outputFilePath),
                            _ => throw new NotSupportedException(),
                        };

                        await reportWriter.WriteReportAsync(results, cancellationToken).ConfigureAwait(false);
                        logger.LogInformation("Report: {outputFilePath} [{format}]", outputFilePath, format);

                        // See the following issues for reasoning behind this check. We want to avoid opening the report
                        // if this process is running as a service or in a CI pipeline.
                        // https://github.com/dotnet/runtime/issues/770#issuecomment-564700467
                        // https://github.com/dotnet/runtime/issues/66530#issuecomment-1065854289
                        bool isRedirected =
                            System.Console.IsInputRedirected &&
                            System.Console.IsOutputRedirected &&
                            System.Console.IsErrorRedirected;

                        bool isInteractive = Environment.UserInteractive && (OperatingSystem.IsWindows() || !isRedirected);

                        if (openReport && isInteractive)
                        {
                            // Open the generated report in the default browser.
                            _ = Process.Start(
                                new ProcessStartInfo
                                {
                                    FileName = outputFilePath,
                                    UseShellExecute = true
                                });
                        }
                    },
                    properties: telemetryProperties)).ConfigureAwait(false);

        return 0;
    }

    private void ReportScenarioRun(
        ScenarioRunResult result,
        UsageDetailsWithTurnCount overallUsageDetails,
        UsageDetailsWithTurnCount wellKnownHostUsageDetails,
        UsageDetailsWithTurnCount localMachineHostUsageDetails,
        Dictionary<(string? model, string? modelProvider), UsageDetailsWithTurnCount> usageDetailsByModel)
    {
        if (result.ChatDetails?.TurnDetails is IList<ChatTurnDetails> turns)
        {
            foreach (ChatTurnDetails turn in turns)
            {
                if (turn.Usage is not null)
                {
                    overallUsageDetails.Add(turn.Usage);

                    if (ModelInfo.IsModelHostWellKnown(turn.ModelProvider))
                    {
                        wellKnownHostUsageDetails.Add(turn.Usage);
                    }
                    else if (ModelInfo.IsModelHostedLocally(turn.ModelProvider))
                    {
                        localMachineHostUsageDetails.Add(turn.Usage);
                    }

                    (string? model, string? modelProvider) key = (turn.Model, turn.ModelProvider);
                    if (!usageDetailsByModel.TryGetValue(key, out UsageDetailsWithTurnCount? usageDetails))
                    {
                        usageDetails = new UsageDetailsWithTurnCount();
                        usageDetailsByModel[key] = usageDetails;
                    }

                    usageDetails.Add(turn.Usage);
                }
            }
        }

        ICollection<EvaluationMetric> metrics = result.EvaluationResult.Metrics.Values;
        string totalMetricsCount = metrics.Count.ToTelemetryPropertyValue();
        string failingMetricsCount = metrics.Count(m => m.Interpretation?.Failed ?? false).ToTelemetryPropertyValue();

        IEnumerable<string> builtInMetricsNames = metrics.Where(m => m.IsBuiltIn()).Select(m => m.Name);
        string builtInMetricsUsed = string.Join(separator: ';', builtInMetricsNames);
        string builtInMetricsCount = builtInMetricsNames.Count().ToTelemetryPropertyValue();

        string totalDiagnosticsCount = metrics.Sum(m => m.Diagnostics?.Count ?? 0).ToTelemetryPropertyValue();

        string errorDiagnosticsCount =
            metrics
                .Sum(m => m.Diagnostics?.Count(d => d.Severity == EvaluationDiagnosticSeverity.Error))
                .ToTelemetryPropertyValue();

        string warningDiagnosticsCount =
            metrics
                .Sum(m => m.Diagnostics?.Count(d => d.Severity == EvaluationDiagnosticSeverity.Warning))
                .ToTelemetryPropertyValue();

        var properties =
            new Dictionary<string, string>
            {
                [TelemetryConstants.PropertyNames.TotalMetricsCount] = totalMetricsCount,
                [TelemetryConstants.PropertyNames.BuiltInMetricsUsed] = builtInMetricsUsed,
                [TelemetryConstants.PropertyNames.BuiltInMetricsCount] = builtInMetricsCount,
                [TelemetryConstants.PropertyNames.FailingMetricsCount] = failingMetricsCount,
                [TelemetryConstants.PropertyNames.TotalDiagnosticsCount] = totalDiagnosticsCount,
                [TelemetryConstants.PropertyNames.ErrorDiagnosticsCount] = errorDiagnosticsCount,
                [TelemetryConstants.PropertyNames.WarningDiagnosticsCount] = warningDiagnosticsCount
            };

        telemetryHelper.ReportEvent(eventName: TelemetryConstants.EventNames.ScenarioRun, properties);
    }

    private void ReportUsageDetails(
        Dictionary<string, string> telemetryProperties,
        UsageDetailsWithTurnCount overallUsageDetails,
        UsageDetailsWithTurnCount wellKnownHostUsageDetails,
        UsageDetailsWithTurnCount localMachineHostUsageDetails,
        Dictionary<(string? model, string? modelProvider), UsageDetailsWithTurnCount> usageDetailsByModel)
    {
        telemetryProperties[TelemetryConstants.PropertyNames.TurnCount] =
            overallUsageDetails.TurnCount.ToTelemetryPropertyValue();
        telemetryProperties[TelemetryConstants.PropertyNames.TotalTokenCount] =
            overallUsageDetails.TotalTokenCount.ToTelemetryPropertyValue();
        telemetryProperties[TelemetryConstants.PropertyNames.InputTokenCount] =
            overallUsageDetails.InputTokenCount.ToTelemetryPropertyValue();

        telemetryProperties[TelemetryConstants.PropertyNames.WellKnownHostTurnCount] =
            wellKnownHostUsageDetails.TurnCount.ToTelemetryPropertyValue();
        telemetryProperties[TelemetryConstants.PropertyNames.WellKnownHostTotalTokenCount] =
            wellKnownHostUsageDetails.TotalTokenCount.ToTelemetryPropertyValue();
        telemetryProperties[TelemetryConstants.PropertyNames.WellKnownHostInputTokenCount] =
            wellKnownHostUsageDetails.InputTokenCount.ToTelemetryPropertyValue();

        telemetryProperties[TelemetryConstants.PropertyNames.LocalMachineHostTurnCount] =
            localMachineHostUsageDetails.TurnCount.ToTelemetryPropertyValue();
        telemetryProperties[TelemetryConstants.PropertyNames.LocalMachineHostTotalTokenCount] =
            localMachineHostUsageDetails.TotalTokenCount.ToTelemetryPropertyValue();
        telemetryProperties[TelemetryConstants.PropertyNames.LocalMachineHostInputTokenCount] =
            localMachineHostUsageDetails.InputTokenCount.ToTelemetryPropertyValue();

        foreach (((string? model, string? modelProvider), UsageDetailsWithTurnCount usageDetails)
            in usageDetailsByModel)
        {
            string isModelHostWellKnown = ModelInfo.IsModelHostWellKnown(modelProvider).ToTelemetryPropertyValue();
            string isModelHostedLocally = ModelInfo.IsModelHostedLocally(modelProvider).ToTelemetryPropertyValue();
            string turnCount = usageDetails.TurnCount.ToTelemetryPropertyValue();
            string totalTokenCount = usageDetails.TotalTokenCount.ToTelemetryPropertyValue();
            string inputTokenCount = usageDetails.InputTokenCount.ToTelemetryPropertyValue();

            var properties =
                new Dictionary<string, string>
                {
                    [TelemetryConstants.PropertyNames.Model] = model.ToTelemetryPropertyValue(),
                    [TelemetryConstants.PropertyNames.ModelProvider] = modelProvider.ToTelemetryPropertyValue(),
                    [TelemetryConstants.PropertyNames.IsModelHostWellKnown] = isModelHostWellKnown,
                    [TelemetryConstants.PropertyNames.IsModelHostedLocally] = isModelHostedLocally,
                    [TelemetryConstants.PropertyNames.TurnCount] = turnCount,
                    [TelemetryConstants.PropertyNames.TotalTokenCount] = totalTokenCount,
                    [TelemetryConstants.PropertyNames.InputTokenCount] = inputTokenCount
                };

            telemetryHelper.ReportEvent(eventName: TelemetryConstants.EventNames.ModelUsageDetails, properties);
        }
    }

    private sealed class UsageDetailsWithTurnCount : UsageDetails
    {
        internal long? TurnCount { get; set; }

        internal new void Add(UsageDetails usageDetails)
        {
            TurnCount =
                usageDetails switch
                {
                    UsageDetailsWithTurnCount usageDetailsWithTurnCount =>
                        NullableSum(TurnCount, usageDetailsWithTurnCount.TurnCount),
                    _ =>
                        NullableSum(TurnCount, 1)
                };

            base.Add(usageDetails);
        }

        private static long? NullableSum(long? a, long? b)
            => (a.HasValue || b.HasValue) ? (a ?? 0) + (b ?? 0) : null;
    }
}

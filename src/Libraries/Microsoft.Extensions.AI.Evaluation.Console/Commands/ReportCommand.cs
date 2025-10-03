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

                        int resultId = 0;
                        var usageDetailsByModel =
                            new Dictionary<(string? model, string? modelProvider), TurnAndTokenUsageDetails>();

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
                                    ReportScenarioRunResult(++resultId, result, usageDetailsByModel);
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

                        ReportUsageDetails(usageDetailsByModel);

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

    private void ReportScenarioRunResult(
        int resultId,
        ScenarioRunResult result,
        Dictionary<(string? model, string? modelProvider), TurnAndTokenUsageDetails> usageDetailsByModel)
    {
        if (result.ChatDetails?.TurnDetails is IList<ChatTurnDetails> turns)
        {
            foreach (ChatTurnDetails turn in turns)
            {
                (string? model, string? modelProvider) key = (turn.Model, turn.ModelProvider);
                if (!usageDetailsByModel.TryGetValue(key, out TurnAndTokenUsageDetails? usageDetails))
                {
                    usageDetails = new TurnAndTokenUsageDetails();
                    usageDetailsByModel[key] = usageDetails;
                }

                usageDetails.Add(turn);
            }
        }

        string resultIdValue = resultId.ToTelemetryPropertyValue();
        ICollection<EvaluationMetric> metrics = result.EvaluationResult.Metrics.Values;

        var properties =
            new Dictionary<string, string>
            {
                [TelemetryConstants.PropertyNames.ScenarioRunResultId] = resultIdValue,
                [TelemetryConstants.PropertyNames.TotalMetricsCount] = metrics.Count.ToTelemetryPropertyValue()
            };

        telemetryHelper.ReportEvent(eventName: TelemetryConstants.EventNames.ScenarioRunResult, properties);

        foreach (EvaluationMetric metric in metrics)
        {
            if (metric.IsBuiltIn())
            {
                ReportBuiltInMetric(metric, resultIdValue);
            }
        }

        void ReportBuiltInMetric(EvaluationMetric metric, string resultIdValue)
        {
            string modelUsed = GetPropertyValueFromMetadata(metric, BuiltInMetricUtilities.ModelUsedMetadataName);
            string inputTokenCount =
                GetPropertyValueFromMetadata(metric, BuiltInMetricUtilities.InputTokensUsedMetadataName);
            string outputTokenCount =
                GetPropertyValueFromMetadata(metric, BuiltInMetricUtilities.OutputTokensUsedMetadataName);
            string durationInSeconds =
                GetPropertyValueFromMetadata(metric, BuiltInMetricUtilities.DurationInSecondsMetadataName);

            string isInterpretedAsFailed = (metric.Interpretation?.Failed).ToTelemetryPropertyValue();
            int errorDiagnosticsCount =
                metric.Diagnostics?.Count(d => d.Severity == EvaluationDiagnosticSeverity.Error) ?? 0;
            int warningDiagnosticsCount =
                metric.Diagnostics?.Count(d => d.Severity == EvaluationDiagnosticSeverity.Warning) ?? 0;
            int informationalDiagnosticsCount =
                metric.Diagnostics?.Count(d => d.Severity == EvaluationDiagnosticSeverity.Informational) ?? 0;

            var properties =
                new Dictionary<string, string>
                {
                    [TelemetryConstants.PropertyNames.MetricName] = metric.Name,
                    [TelemetryConstants.PropertyNames.ScenarioRunResultId] = resultIdValue,
                    [TelemetryConstants.PropertyNames.ModelUsed] = modelUsed,
                    [TelemetryConstants.PropertyNames.InputTokenCount] = inputTokenCount,
                    [TelemetryConstants.PropertyNames.OutputTokenCount] = outputTokenCount,
                    [TelemetryConstants.PropertyNames.DurationInSeconds] = durationInSeconds,
                    [TelemetryConstants.PropertyNames.IsInterpretedAsFailed] = isInterpretedAsFailed,
                    [TelemetryConstants.PropertyNames.ErrorDiagnosticsCount] =
                        errorDiagnosticsCount.ToTelemetryPropertyValue(),
                    [TelemetryConstants.PropertyNames.WarningDiagnosticsCount] =
                        warningDiagnosticsCount.ToTelemetryPropertyValue(),
                    [TelemetryConstants.PropertyNames.InformationalDiagnosticsCount] =
                        informationalDiagnosticsCount.ToTelemetryPropertyValue()
                };

            telemetryHelper.ReportEvent(eventName: TelemetryConstants.EventNames.BuiltInMetric, properties);

            static string GetPropertyValueFromMetadata(EvaluationMetric metric, string metadataName)
            {
                string? metadataValue = null;
                _ = metric.Metadata?.TryGetValue(metadataName, out metadataValue);
                return metadataValue.ToTelemetryPropertyValue();
            }
        }
    }

    private void ReportUsageDetails(
        Dictionary<(string? model, string? modelProvider), TurnAndTokenUsageDetails> usageDetailsByModel)
    {
        foreach (((string? model, string? modelProvider), TurnAndTokenUsageDetails usageDetails)
            in usageDetailsByModel)
        {
            string isModelHostWellKnown = ModelInfo.IsModelHostWellKnown(modelProvider).ToTelemetryPropertyValue();
            string isModelHostedLocally = ModelInfo.IsModelHostedLocally(modelProvider).ToTelemetryPropertyValue();
            string cachedTurnCount = usageDetails.CachedTurnCount.ToTelemetryPropertyValue();
            string nonCachedTurnCount = usageDetails.NonCachedTurnCount.ToTelemetryPropertyValue();
            string cachedInputTokenCount = usageDetails.CachedInputTokenCount.ToTelemetryPropertyValue();
            string nonCachedInputTokenCount = usageDetails.NonCachedInputTokenCount.ToTelemetryPropertyValue();
            string cachedOutputTokenCount = usageDetails.CachedOutputTokenCount.ToTelemetryPropertyValue();
            string nonCachedOutputTokenCount = usageDetails.NonCachedOutputTokenCount.ToTelemetryPropertyValue();

            var properties =
                new Dictionary<string, string>
                {
                    [TelemetryConstants.PropertyNames.Model] = model.ToTelemetryPropertyValue(),
                    [TelemetryConstants.PropertyNames.ModelProvider] = modelProvider.ToTelemetryPropertyValue(),
                    [TelemetryConstants.PropertyNames.IsModelHostWellKnown] = isModelHostWellKnown,
                    [TelemetryConstants.PropertyNames.IsModelHostedLocally] = isModelHostedLocally,
                    [TelemetryConstants.PropertyNames.CachedTurnCount] = cachedTurnCount,
                    [TelemetryConstants.PropertyNames.NonCachedTurnCount] = nonCachedTurnCount,
                    [TelemetryConstants.PropertyNames.CachedInputTokenCount] = cachedInputTokenCount,
                    [TelemetryConstants.PropertyNames.NonCachedInputTokenCount] = nonCachedInputTokenCount,
                    [TelemetryConstants.PropertyNames.CachedOutputTokenCount] = cachedOutputTokenCount,
                    [TelemetryConstants.PropertyNames.NonCachedOutputTokenCount] = nonCachedOutputTokenCount
                };

            telemetryHelper.ReportEvent(eventName: TelemetryConstants.EventNames.ModelUsageDetails, properties);
        }
    }

    private sealed class TurnAndTokenUsageDetails
    {
        internal long CachedTurnCount { get; set; }
        internal long NonCachedTurnCount { get; set; }
        internal long? CachedInputTokenCount { get; set; }
        internal long? NonCachedInputTokenCount { get; set; }
        internal long? CachedOutputTokenCount { get; set; }
        internal long? NonCachedOutputTokenCount { get; set; }

        internal void Add(ChatTurnDetails turn)
        {
            bool isCached = turn.CacheHit ?? false;
            if (isCached)
            {
                ++CachedTurnCount;
                if (turn.Usage is not null)
                {
                    if (turn.Usage.InputTokenCount is not null)
                    {
                        CachedInputTokenCount += turn.Usage.InputTokenCount;
                    }

                    if (turn.Usage.OutputTokenCount is not null)
                    {
                        CachedOutputTokenCount += turn.Usage.OutputTokenCount;
                    }
                }
            }
            else
            {
                ++NonCachedTurnCount;
                if (turn.Usage is not null)
                {
                    if (turn.Usage.InputTokenCount is not null)
                    {
                        NonCachedInputTokenCount += turn.Usage.InputTokenCount;
                    }

                    if (turn.Usage.OutputTokenCount is not null)
                    {
                        NonCachedOutputTokenCount += turn.Usage.OutputTokenCount;
                    }
                }
            }
        }
    }
}

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
using static Microsoft.Extensions.AI.Evaluation.Console.Telemetry.TelemetryConstants;

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
                [PropertyNames.LastN] = lastN.ToTelemetryPropertyValue(),
                [PropertyNames.Format] = format.ToString(),
                [PropertyNames.OpenReport] = openReport.ToTelemetryPropertyValue()
            };

        await logger.ExecuteWithCatchAsync(
            operation: () =>
                telemetryHelper.ReportOperationAsync(
                    operationName: EventNames.ReportCommand,
                    operation: async ValueTask () =>
                    {
                        IEvaluationResultStore resultStore;

                        if (storageRootDir is not null)
                        {
                            string storageRootPath = storageRootDir.FullName;
                            logger.LogInformation("Storage root path: {StorageRootPath}", storageRootPath);

                            resultStore = new DiskBasedResultStore(storageRootPath);

                            telemetryProperties[PropertyNames.StorageType] = PropertyValues.StorageTypeDisk;
                        }
                        else if (endpointUri is not null)
                        {
                            logger.LogInformation("Azure Storage endpoint: {EndpointUri}", endpointUri);

                            var fsClient = new DataLakeDirectoryClient(endpointUri, new DefaultAzureCredential());
                            resultStore = new AzureStorageResultStore(fsClient);

                            telemetryProperties[PropertyNames.StorageType] = PropertyValues.StorageTypeAzure;
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
                                    ReportScenarioRunResult(
                                        ++resultId,
                                        result,
                                        usageDetailsByModel,
                                        cancellationToken);
                                }
                                else
                                {
                                    // Clear the chat data for following executions
                                    result.Messages = [];
                                    result.ModelResponse = new ChatResponse();
                                }

                                results.Add(result);

                                logger.LogInformation(
                                    "Execution: {ExecutionName} Scenario: {ScenarioName} Iteration: {IterationName}",
                                    result.ExecutionName,
                                    result.ScenarioName,
                                    result.IterationName);
                            }
                        }

                        ReportUsageDetails(usageDetailsByModel, cancellationToken);

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
                        logger.LogInformation("Report: {OutputFilePath} [{Format}]", outputFilePath, format);

                        // See the following issues for reasoning behind this check. We want to avoid opening the
                        // report if this process is running as a service or in a CI pipeline.
                        // https://github.com/dotnet/runtime/issues/770#issuecomment-564700467
                        // https://github.com/dotnet/runtime/issues/66530#issuecomment-1065854289
                        bool isRedirected =
                            System.Console.IsInputRedirected &&
                            System.Console.IsOutputRedirected &&
                            System.Console.IsErrorRedirected;

                        bool isInteractive =
                            Environment.UserInteractive && (OperatingSystem.IsWindows() || !isRedirected);

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
                    properties: telemetryProperties,
                    logger: logger)).ConfigureAwait(false);

        return 0;
    }

    private void ReportScenarioRunResult(
        int resultId,
        ScenarioRunResult result,
        Dictionary<(string? model, string? modelProvider), TurnAndTokenUsageDetails> usageDetailsByModel,
        CancellationToken cancellationToken)
    {
        logger.ExecuteWithCatch(() =>
        {
            if (result.ChatDetails?.TurnDetails is IList<ChatTurnDetails> turns)
            {
                foreach (ChatTurnDetails turn in turns)
                {
                    cancellationToken.ThrowIfCancellationRequested();

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
                    [PropertyNames.ScenarioRunResultId] = resultIdValue,
                    [PropertyNames.MetricsCount] = metrics.Count.ToTelemetryPropertyValue()
                };

            telemetryHelper.ReportEvent(eventName: EventNames.ScenarioRunResult, properties);

            foreach (EvaluationMetric metric in metrics)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (metric.IsBuiltIn())
                {
                    ReportBuiltInMetric(metric, resultIdValue);
                }
            }
        },
        swallowUnhandledExceptions: true); // Log and ignore exceptions encountered when trying to report telemetry.

        void ReportBuiltInMetric(EvaluationMetric metric, string resultIdValue)
        {
            // We always want to report the diagnostics counts - even when metric.Diagnostics is null. This is because
            // we know that when metric.Diagnostics is null, that means there were no diagnostics (as opposed to
            // meaning that diagnostic information was somehow missing or unavailable).
            int errorDiagnosticsCount =
                metric.Diagnostics?.Count(d => d.Severity == EvaluationDiagnosticSeverity.Error) ?? 0;
            int warningDiagnosticsCount =
                metric.Diagnostics?.Count(d => d.Severity == EvaluationDiagnosticSeverity.Warning) ?? 0;
            int informationalDiagnosticsCount =
                metric.Diagnostics?.Count(d => d.Severity == EvaluationDiagnosticSeverity.Informational) ?? 0;

            var properties =
                new Dictionary<string, string>
                {
                    [PropertyNames.MetricName] = metric.Name,
                    [PropertyNames.ScenarioRunResultId] = resultIdValue,
                    [PropertyNames.ErrorDiagnosticsCount] = errorDiagnosticsCount.ToTelemetryPropertyValue(),
                    [PropertyNames.WarningDiagnosticsCount] = warningDiagnosticsCount.ToTelemetryPropertyValue(),
                    [PropertyNames.InformationalDiagnosticsCount] =
                        informationalDiagnosticsCount.ToTelemetryPropertyValue()
                };

            // We want to omit reporting the below properties (such as token counts) when the corresponding metadata is
            // missing. This is because we know that when the metadata is missing, that means the corresponding
            // information was not available. For example, it would be wrong to report the token counts as 0 when in
            // reality the token count information is missing because it was not available as part of the ChatResponse
            // returned from the IChatClient during evaluation.
            if (TryGetPropertyValueFromMetadata(BuiltInMetricUtilities.EvalModelMetadataName) is string model)
            {
                properties[PropertyNames.Model] = model;
            }

            if (TryGetPropertyValueFromMetadata(BuiltInMetricUtilities.EvalInputTokensMetadataName)
                    is string inputTokenCount)
            {
                properties[PropertyNames.InputTokenCount] = inputTokenCount;
            }

            if (TryGetPropertyValueFromMetadata(BuiltInMetricUtilities.EvalOutputTokensMetadataName)
                    is string outputTokenCount)
            {
                properties[PropertyNames.OutputTokenCount] = outputTokenCount;
            }

            if (TryGetPropertyValueFromMetadata(BuiltInMetricUtilities.EvalDurationMillisecondsMetadataName)
                    is string durationInMilliseconds)
            {
                properties[PropertyNames.DurationInMilliseconds] = durationInMilliseconds;
            }

            if (metric.Interpretation?.Failed is bool failed)
            {
                properties[PropertyNames.IsInterpretedAsFailed] = failed.ToTelemetryPropertyValue();
            }

            telemetryHelper.ReportEvent(eventName: EventNames.BuiltInMetric, properties);

            string? TryGetPropertyValueFromMetadata(string metadataName)
            {
                if ((metric.Metadata?.TryGetValue(metadataName, out string? value)) is not true ||
                    string.IsNullOrWhiteSpace(value))
                {
                    return null;
                }

                return value;
            }
        }
    }

    private void ReportUsageDetails(
        Dictionary<(string? model, string? modelProvider), TurnAndTokenUsageDetails> usageDetailsByModel,
        CancellationToken cancellationToken)
    {
        logger.ExecuteWithCatch(() =>
        {
            foreach (((string? model, string? modelProvider), TurnAndTokenUsageDetails usageDetails)
                in usageDetailsByModel)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string isModelHostWellKnown = ModelInfo.IsModelHostWellKnown(modelProvider).ToTelemetryPropertyValue();
                string isModelHostedLocally = ModelInfo.IsModelHostedLocally(modelProvider).ToTelemetryPropertyValue();
                string cachedTurnCount = usageDetails.CachedTurnCount.ToTelemetryPropertyValue();
                string nonCachedTurnCount = usageDetails.NonCachedTurnCount.ToTelemetryPropertyValue();

                var properties =
                    new Dictionary<string, string>
                    {
                        [PropertyNames.Model] = model.ToTelemetryPropertyValue(defaultValue: PropertyValues.Unknown),
                        [PropertyNames.ModelProvider] =
                            modelProvider.ToTelemetryPropertyValue(defaultValue: PropertyValues.Unknown),
                        [PropertyNames.IsModelHostWellKnown] = isModelHostWellKnown,
                        [PropertyNames.IsModelHostedLocally] = isModelHostedLocally,
                        [PropertyNames.CachedTurnCount] = cachedTurnCount,
                        [PropertyNames.NonCachedTurnCount] = nonCachedTurnCount
                    };

                // We want to omit reporting the below token counts when the information is not available. It would be
                // wrong to report the token counts as 0 when in reality the token count information is missing because
                // it was not available as part of the ChatResponses returned from the IChatClients used during
                // evaluation.
                if (usageDetails.CachedInputTokenCount is long cachedInputTokenCount)
                {
                    properties[PropertyNames.CachedInputTokenCount] = cachedInputTokenCount.ToTelemetryPropertyValue();
                }

                if (usageDetails.CachedOutputTokenCount is long cachedOutputTokenCount)
                {
                    properties[PropertyNames.CachedOutputTokenCount] =
                        cachedOutputTokenCount.ToTelemetryPropertyValue();
                }

                if (usageDetails.NonCachedInputTokenCount is long nonCachedInputTokenCount)
                {
                    properties[PropertyNames.NonCachedInputTokenCount] =
                        nonCachedInputTokenCount.ToTelemetryPropertyValue();
                }

                if (usageDetails.NonCachedOutputTokenCount is long nonCachedOutputTokenCount)
                {
                    properties[PropertyNames.NonCachedOutputTokenCount] =
                        nonCachedOutputTokenCount.ToTelemetryPropertyValue();
                }

                telemetryHelper.ReportEvent(eventName: EventNames.ModelUsageDetails, properties);
            }
        },
        swallowUnhandledExceptions: true); // Log and ignore exceptions encountered when trying to report telemetry.
    }

    private sealed class TurnAndTokenUsageDetails
    {
        internal long CachedTurnCount { get; private set; }
        internal long NonCachedTurnCount { get; private set; }
        internal long? CachedInputTokenCount { get; private set; }
        internal long? NonCachedInputTokenCount { get; private set; }
        internal long? CachedOutputTokenCount { get; private set; }
        internal long? NonCachedOutputTokenCount { get; private set; }

        internal void Add(ChatTurnDetails turn)
        {
            EnsureTokenCountsInitialized();

            bool isCached = turn.CacheHit ?? false;
            if (isCached)
            {
                ++CachedTurnCount;
                CachedInputTokenCount += turn.Usage?.InputTokenCount;
                CachedOutputTokenCount += turn.Usage?.OutputTokenCount;
            }
            else
            {
                ++NonCachedTurnCount;
                NonCachedInputTokenCount += turn.Usage?.InputTokenCount;
                NonCachedOutputTokenCount += turn.Usage?.OutputTokenCount;
            }

            void EnsureTokenCountsInitialized()
            {
                // If any turn (for a particular model and model provider combination) contains token usage details, we
                // initialize both the cumulative cached token counts as well as the cumulative non-cached token counts
                // (for this model and model provider combination) to 0. This is done so that when all token usage (for
                // a particular model and model provider combination) is non-cached, we can report the cumulative
                // cached token counts (for this model and model provider combination) as 0 (as opposed to treating the
                // cached token counts as unknown and omitting them from the reported event), and vice-versa. The
                // assumption here is that if any turn (for a particular model and model provider combination) contains
                // token usage details, then all other turns (for the same model and model provider combination) will
                // also contain this.

                if (turn.Usage?.InputTokenCount is not null)
                {
                    CachedInputTokenCount ??= 0;
                    NonCachedInputTokenCount ??= 0;
                }

                if (turn.Usage?.OutputTokenCount is not null)
                {
                    CachedOutputTokenCount ??= 0;
                    NonCachedOutputTokenCount ??= 0;
                }
            }
        }
    }
}

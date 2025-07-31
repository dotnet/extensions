// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Html;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Json;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI.Evaluation.Console.Commands;

internal sealed partial class ReportCommand(ILogger logger)
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
        IEvaluationResultStore resultStore;

        if (storageRootDir is not null)
        {
            string storageRootPath = storageRootDir.FullName;
            logger.LogInformation("Storage root path: {storageRootPath}", storageRootPath);

            resultStore = new DiskBasedResultStore(storageRootPath);
        }
        else if (endpointUri is not null)
        {
            logger.LogInformation("Azure Storage endpoint: {endpointUri}", endpointUri);

            var fsClient = new DataLakeDirectoryClient(endpointUri, new DefaultAzureCredential());
            resultStore = new AzureStorageResultStore(fsClient);
        }
        else
        {
            throw new InvalidOperationException("Either --path or --endpoint must be specified");
        }

        List<ScenarioRunResult> results = [];

        string? latestExecutionName = null;

        await foreach (string executionName in
            resultStore.GetLatestExecutionNamesAsync(lastN, cancellationToken).ConfigureAwait(false))
        {
            latestExecutionName ??= executionName;

            await foreach (ScenarioRunResult result in
                resultStore.ReadResultsAsync(
                    executionName,
                    cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (result.ExecutionName != latestExecutionName)
                {
                    // Clear the chat data for following executions
                    result.Messages = [];
                    result.ModelResponse = new ChatResponse();
                }

                results.Add(result);

                logger.LogInformation("Execution: {executionName} Scenario: {scenarioName} Iteration: {iterationName}", result.ExecutionName, result.ScenarioName, result.IterationName);
            }
        }

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
        bool isRedirected = System.Console.IsInputRedirected && System.Console.IsOutputRedirected && System.Console.IsErrorRedirected;
        bool isInteractive = Environment.UserInteractive && (OperatingSystem.IsWindows() || !(isRedirected));

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

        return 0;
    }
}

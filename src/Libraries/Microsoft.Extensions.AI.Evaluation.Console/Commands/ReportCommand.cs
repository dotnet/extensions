// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Html;
using Microsoft.Extensions.AI.Evaluation.Reporting.Formats.Json;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI.Evaluation.Console.Commands;

internal sealed partial class ReportCommand(ILogger logger)
{
    internal async Task<int> InvokeAsync(
        DirectoryInfo storageRootDir,
        FileInfo outputFile,
        int lastN,
        Format format,
        CancellationToken cancellationToken = default)
    {
        string storageRootPath = storageRootDir.FullName;
        logger.LogInformation("Storage root path: {storageRootPath}", storageRootPath);

        var results = new List<ScenarioRunResult>();
        var resultStore = new DiskBasedResultStore(storageRootPath);

        await foreach (string executionName in
            resultStore.GetLatestExecutionNamesAsync(lastN, cancellationToken).ConfigureAwait(false))
        {
            await foreach (ScenarioRunResult result in
                resultStore.ReadResultsAsync(
                    executionName,
                    cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                results.Add(result);
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

        return 0;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Microsoft.Extensions.AI.Evaluation.Console.Utilities;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI.Evaluation.Console.Commands;

internal sealed class CleanResultsCommand(ILogger logger)
{
    internal async Task<int> InvokeAsync(
        DirectoryInfo? storageRootDir,
        Uri? endpointUri,
        int lastN,
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

        await logger.ExecuteWithCatchAsync(
            async ValueTask () =>
            {
                if (lastN is 0)
                {
                    logger.LogInformation("Deleting all results...");

                    await resultStore.DeleteResultsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    logger.LogInformation("Deleting all results except the {lastN} most recent ones...", lastN);

                    HashSet<string> toPreserve = [];

                    await foreach (string executionName in
                        resultStore.GetLatestExecutionNamesAsync(lastN, cancellationToken).ConfigureAwait(false))
                    {
                        _ = toPreserve.Add(executionName);
                    }

                    await foreach (string executionName in
                        resultStore.GetLatestExecutionNamesAsync(
                            cancellationToken: cancellationToken).ConfigureAwait(false))
                    {
                        if (!toPreserve.Contains(executionName))
                        {
                            await resultStore.DeleteResultsAsync(
                                executionName,
                                cancellationToken: cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }).ConfigureAwait(false);

        return 0;
    }
}

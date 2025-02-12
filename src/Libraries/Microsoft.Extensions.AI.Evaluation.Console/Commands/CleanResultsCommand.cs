// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Console.Utilities;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI.Evaluation.Console.Commands;

internal sealed class CleanResultsCommand(ILogger logger)
{
    internal async Task<int> InvokeAsync(
        DirectoryInfo storageRootDir,
        int lastN,
        CancellationToken cancellationToken = default)
    {
        string storageRootPath = storageRootDir.FullName;
        logger.LogInformation("Storage root path: {storageRootPath}", storageRootPath);

        var resultStore = new DiskBasedResultStore(storageRootPath);

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

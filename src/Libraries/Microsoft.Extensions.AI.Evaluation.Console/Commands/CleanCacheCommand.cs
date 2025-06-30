// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

internal sealed class CleanCacheCommand(ILogger logger)
{
    internal async Task<int> InvokeAsync(DirectoryInfo? storageRootDir, Uri? endpointUri, CancellationToken cancellationToken = default)
    {
        IEvaluationResponseCacheProvider cacheProvider;

        if (storageRootDir is not null)
        {
            string storageRootPath = storageRootDir.FullName;
            logger.LogInformation("Storage root path: {storageRootPath}", storageRootPath);
            logger.LogInformation("Deleting expired cache entries...");

            cacheProvider = new DiskBasedResponseCacheProvider(storageRootPath);
        }
        else if (endpointUri is not null)
        {
            logger.LogInformation("Azure Storage endpoint: {endpointUri}", endpointUri);

            var fsClient = new DataLakeDirectoryClient(endpointUri, new DefaultAzureCredential());
            cacheProvider = new AzureStorageResponseCacheProvider(fsClient);
        }
        else
        {
            throw new InvalidOperationException("Either --path or --endpoint must be specified");
        }

        await logger.ExecuteWithCatchAsync(
            () => cacheProvider.DeleteExpiredCacheEntriesAsync(cancellationToken)).ConfigureAwait(false);

        return 0;
    }
}

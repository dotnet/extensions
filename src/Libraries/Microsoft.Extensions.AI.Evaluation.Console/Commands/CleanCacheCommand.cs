// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Storage.Files.DataLake;
using Microsoft.Extensions.AI.Evaluation.Console.Telemetry;
using Microsoft.Extensions.AI.Evaluation.Console.Utilities;
using Microsoft.Extensions.AI.Evaluation.Reporting;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Logging;
using static Microsoft.Extensions.AI.Evaluation.Console.Telemetry.TelemetryConstants;

namespace Microsoft.Extensions.AI.Evaluation.Console.Commands;

internal sealed class CleanCacheCommand(ILogger logger, TelemetryHelper telemetryHelper)
{
    internal async Task<int> InvokeAsync(
        DirectoryInfo? storageRootDir,
        Uri? endpointUri,
        CancellationToken cancellationToken = default)
    {
        var telemetryProperties = new Dictionary<string, string>();

        await logger.ExecuteWithCatchAsync(
            operation: () =>
                telemetryHelper.ReportOperationAsync(
                    operationName: EventNames.CleanCacheCommand,
                    operation: () =>
                    {
                        IEvaluationResponseCacheProvider cacheProvider;

                        if (storageRootDir is not null)
                        {
                            string storageRootPath = storageRootDir.FullName;
                            logger.LogInformation("Storage root path: {storageRootPath}", storageRootPath);
                            logger.LogInformation("Deleting expired cache entries...");

                            cacheProvider = new DiskBasedResponseCacheProvider(storageRootPath);

                            telemetryProperties[PropertyNames.StorageType] = PropertyValues.StorageTypeDisk;
                        }
                        else if (endpointUri is not null)
                        {
                            logger.LogInformation("Azure Storage endpoint: {endpointUri}", endpointUri);

                            var fsClient = new DataLakeDirectoryClient(endpointUri, new DefaultAzureCredential());
                            cacheProvider = new AzureStorageResponseCacheProvider(fsClient);

                            telemetryProperties[PropertyNames.StorageType] = PropertyValues.StorageTypeAzure;
                        }
                        else
                        {
                            throw new InvalidOperationException("Either --path or --endpoint must be specified");
                        }

                        return cacheProvider.DeleteExpiredCacheEntriesAsync(cancellationToken);
                    },
                    properties: telemetryProperties,
                    logger: logger)).ConfigureAwait(false);

        return 0;
    }
}

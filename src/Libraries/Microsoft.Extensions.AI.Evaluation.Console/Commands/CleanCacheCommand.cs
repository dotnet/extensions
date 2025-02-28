// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Console.Utilities;
using Microsoft.Extensions.AI.Evaluation.Reporting.Storage;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.AI.Evaluation.Console.Commands;

internal sealed class CleanCacheCommand(ILogger logger)
{
    internal async Task<int> InvokeAsync(DirectoryInfo storageRootDir, CancellationToken cancellationToken = default)
    {
        string storageRootPath = storageRootDir.FullName;
        logger.LogInformation("Storage root path: {storageRootPath}", storageRootPath);
        logger.LogInformation("Deleting expired cache entries...");

        var cacheProvider = new DiskBasedResponseCacheProvider(storageRootPath);

        await logger.ExecuteWithCatchAsync(
            () => cacheProvider.DeleteExpiredCacheEntriesAsync(cancellationToken)).ConfigureAwait(false);

        return 0;
    }
}

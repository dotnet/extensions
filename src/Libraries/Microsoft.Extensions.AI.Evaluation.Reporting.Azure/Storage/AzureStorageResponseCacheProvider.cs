// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Files.DataLake;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

/// <summary>
/// An <see cref="IResponseCacheProvider"/> that returns a <see cref="AzureStorageResponseCache"/>.
/// </summary>
/// <param name="client">
/// A <see cref="DataLakeDirectoryClient"/> with access to an Azure Storage container under which the cached AI
/// responses should be stored.
/// </param>
/// <param name="timeToLiveForCacheEntries">
/// An optional <see cref="TimeSpan"/> that specifies the maximum amount of time that cached AI responses should
/// survive in the cache before they are considered expired and evicted.
/// </param>
public sealed class AzureStorageResponseCacheProvider(
    DataLakeDirectoryClient client,
    TimeSpan? timeToLiveForCacheEntries = null) : IResponseCacheProvider
{
    private readonly Func<DateTime> _provideDateTime = () => DateTime.Now;

    /// <remarks>
    /// Intended for testing purposes only.
    /// </remarks>
    internal AzureStorageResponseCacheProvider(
        DataLakeDirectoryClient client,
        TimeSpan? timeToLiveForCacheEntries,
        Func<DateTime> provideDateTime)
            : this(client, timeToLiveForCacheEntries)
    {
        _provideDateTime = provideDateTime;
    }

    /// <inheritdoc/>
    public ValueTask<IDistributedCache> GetCacheAsync(
        string scenarioName,
        string iterationName,
        CancellationToken cancellationToken = default)
    {
        var cache =
            new AzureStorageResponseCache(
                client,
                scenarioName,
                iterationName,
                timeToLiveForCacheEntries,
                _provideDateTime);

        return new ValueTask<IDistributedCache>(cache);
    }

    /// <inheritdoc/>
    public ValueTask ResetAsync(CancellationToken cancellationToken = default)
        => AzureStorageResponseCache.ResetStorageAsync(client, cancellationToken);

    /// <inheritdoc/>
    public ValueTask DeleteExpiredCacheEntriesAsync(CancellationToken cancellationToken = default)
        => AzureStorageResponseCache.DeleteExpiredEntriesAsync(client, _provideDateTime, cancellationToken);
}

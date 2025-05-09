// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

/// <summary>
/// An <see cref="IEvaluationResponseCacheProvider"/> that returns an <see cref="IDistributedCache"/> that can cache
/// AI responses for a particular <see cref="ScenarioRun"/> under the specified <paramref name="storageRootPath"/> on
/// disk.
/// </summary>
/// <param name="storageRootPath">
/// The path to a directory on disk under which the cached AI responses should be stored.
/// </param>
/// <param name="timeToLiveForCacheEntries">
/// An optional <see cref="TimeSpan"/> that specifies the maximum amount of time that cached AI responses should
/// survive in the cache before they are considered expired and evicted.
/// </param>
public sealed class DiskBasedResponseCacheProvider(
    string storageRootPath,
    TimeSpan? timeToLiveForCacheEntries = null) : IEvaluationResponseCacheProvider
{
    private readonly Func<DateTime> _provideDateTime = () => DateTime.UtcNow;

    /// <remarks>
    /// Intended for testing purposes only.
    /// </remarks>
    internal DiskBasedResponseCacheProvider(
        string storageRootPath,
        Func<DateTime> provideDateTime,
        TimeSpan? timeToLiveForCacheEntries = null)
            : this(storageRootPath, timeToLiveForCacheEntries)
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
            new DiskBasedResponseCache(
                storageRootPath,
                scenarioName,
                iterationName,
                _provideDateTime,
                timeToLiveForCacheEntries);

        return new ValueTask<IDistributedCache>(cache);
    }

    /// <inheritdoc/>
    public ValueTask ResetAsync(CancellationToken cancellationToken = default)
    {
        DiskBasedResponseCache.ResetStorage(storageRootPath);

        return default;
    }

    /// <inheritdoc/>
    public ValueTask DeleteExpiredCacheEntriesAsync(CancellationToken cancellationToken = default)
        => DiskBasedResponseCache.DeleteExpiredEntriesAsync(storageRootPath, _provideDateTime, cancellationToken);
}

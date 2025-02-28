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
/// An <see cref="IResponseCacheProvider"/> that returns a <see cref="DiskBasedResponseCache"/>.
/// </summary>
/// <param name="storageRootPath">
/// The path to a directory on disk under which the cached AI responses should be stored.
/// </param>
public sealed class DiskBasedResponseCacheProvider(string storageRootPath) : IResponseCacheProvider
{
    private readonly Func<DateTime> _provideDateTime = () => DateTime.UtcNow;

    /// <remarks>
    /// Intended for testing purposes only.
    /// </remarks>
    internal DiskBasedResponseCacheProvider(string storageRootPath, Func<DateTime> provideDateTime)
        : this(storageRootPath)
    {
        _provideDateTime = provideDateTime;
    }

    /// <inheritdoc/>
    public ValueTask<IDistributedCache> GetCacheAsync(
        string scenarioName,
        string iterationName,
        CancellationToken cancellationToken = default)
    {
        var cache = new DiskBasedResponseCache(storageRootPath, scenarioName, iterationName, _provideDateTime);

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

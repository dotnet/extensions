// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

#pragma warning disable CA1725
// CA1725: Parameter names should match base declaration.
// All functions on 'IDistributedCache' use the parameter name 'token' in place of 'cancellationToken'. However,
// changing the name of the corresponding parameters below to 'token' (in order to fix CA1725) would make the names
// inconsistent with the rest of the codebase. So we suppress this warning.

using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Azure.Storage.Files.DataLake.Specialized;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

/// <summary>
/// An <see cref="IDistributedCache"/> implementation that stores cached AI responses for a particular
/// <see cref="ScenarioRun"/> under an Azure Storage container.
/// </summary>
/// <param name="client">
/// A <see cref="DataLakeDirectoryClient"/> with access to an Azure Storage container under which the cached AI
/// responses should be stored.
/// </param>
/// <param name="scenarioName">
/// The <see cref="ScenarioRun.ScenarioName"/> for the returned <see cref="AzureStorageResponseCache"/> instance.
/// </param>
/// <param name="iterationName">
/// The <see cref="ScenarioRun.IterationName"/> for the returned <see cref="AzureStorageResponseCache"/> instance.
/// </param>
/// <param name="timeToLiveForCacheEntries">
/// An optional <see cref="TimeSpan"/> that specifies the maximum amount of time that cached AI responses should
/// survive in the cache before they are considered expired and evicted.
/// </param>
public sealed partial class AzureStorageResponseCache(
    DataLakeDirectoryClient client,
    string scenarioName,
    string iterationName,
    TimeSpan? timeToLiveForCacheEntries = null) : IDistributedCache
{
    private const string EntryFileName = "entry.json";
    private const string ContentsFileName = "contents.data";

    private const string EntryFileNotFound = "Cache entry file {0} was not found.";
    private const string ContentsFileNotFound = "Cache contents file {0} was not found.";
    private const string EntryAndContentsFilesNotFound = "Cache entry file {0} and contents file {1} were not found.";

    private readonly string _iterationPath = $"cache/{scenarioName}/{iterationName}";
    private readonly TimeSpan _timeToLiveForCacheEntries =
        timeToLiveForCacheEntries ?? Defaults.DefaultTimeToLiveForCacheEntries;
    private readonly Func<DateTime> _provideDateTime = () => DateTime.UtcNow;

    /// <remarks>
    /// Intended for testing purposes only.
    /// </remarks>
    internal AzureStorageResponseCache(
        DataLakeDirectoryClient client,
        string scenarioName,
        string iterationName,
        TimeSpan? timeToLiveForCacheEntries,
        Func<DateTime> provideDateTime)
            : this(client, scenarioName, iterationName, timeToLiveForCacheEntries)
    {
        _provideDateTime = provideDateTime;
    }

    /// <inheritdoc/>
    public byte[]? Get(string key)
    {
        (string entryFilePath, string contentsFilePath, bool filesExist) = CheckPaths(key);

        if (!filesExist)
        {
            return null;
        }

        CacheEntry entry = CacheEntry.Read(client.GetFileClient(entryFilePath));
        if (entry.Expiration <= _provideDateTime())
        {
            Remove(key);
            return null;
        }

        return client.GetFileClient(contentsFilePath).ReadContent().Value.Content.ToArray();
    }

    /// <inheritdoc/>
    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        (string entryFilePath, string contentsFilePath, bool filesExist) =
            await CheckPathsAsync(key, cancellationToken).ConfigureAwait(false);

        if (!filesExist)
        {
            return null;
        }

        CacheEntry entry =
            await CacheEntry.ReadAsync(
                client.GetFileClient(entryFilePath),
                cancellationToken: cancellationToken).ConfigureAwait(false);

        if (entry.Expiration <= _provideDateTime())
        {
            await RemoveAsync(key, cancellationToken).ConfigureAwait(false);

            return null;
        }

        Response<DataLakeFileReadResult> content =
            await client.GetFileClient(contentsFilePath).ReadContentAsync(cancellationToken).ConfigureAwait(false);

        return content.Value.Content.ToArray();
    }

    /// <inheritdoc/>
    public void Refresh(string key)
    {
        (string entryFilePath, string contentsFilePath, bool filesExist) = CheckPaths(key);

        if (!filesExist)
        {
            throw new FileNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    EntryAndContentsFilesNotFound,
                    entryFilePath,
                    contentsFilePath));
        }

        DataLakeFileClient entryFileClient = client.GetFileClient(entryFilePath);

        CacheEntry entry = CreateEntry();
        entry.Write(entryFileClient);
    }

    /// <inheritdoc/>
    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        (string entryFilePath, string contentsFilePath, bool filesExist) =
            await CheckPathsAsync(key, cancellationToken).ConfigureAwait(false);

        if (!filesExist)
        {
            throw new FileNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    EntryAndContentsFilesNotFound,
                    entryFilePath,
                    contentsFilePath));
        }

        DataLakeFileClient entryClient = client.GetFileClient(entryFilePath);

        CacheEntry entry = CreateEntry();
        await entry.WriteAsync(entryClient, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Remove(string key)
    {
        (string entryFilePath, string contentsFilePath) = GetPaths(key);

        DataLakeFileClient entryClient = client.GetFileClient(entryFilePath);
        DataLakeFileClient contentsClient = client.GetFileClient(contentsFilePath);

        _ = entryClient.Delete();
        _ = contentsClient.Delete();
    }

    /// <inheritdoc/>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        (string entryFilePath, _) = GetPaths(key);

        DataLakeDirectoryClient keyDirClient = client.GetFileClient(entryFilePath).GetParentDirectoryClient();

        _ = await keyDirClient.DeleteAsync(
                recursive: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        (string entryFilePath, string contentsFilePath) = GetPaths(key);

        DataLakeFileClient entryClient = client.GetFileClient(entryFilePath);
        DataLakeFileClient contentsClient = client.GetFileClient(contentsFilePath);

        CacheEntry entry = CreateEntry();
        entry.Write(entryClient);

        _ = contentsClient.Upload(BinaryData.FromBytes(value).ToStream(), overwrite: true);
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken cancellationToken = default)
    {
        (string entryFilePath, string contentsFilePath) = GetPaths(key);

        DataLakeFileClient entryClient = client.GetFileClient(entryFilePath);
        DataLakeFileClient contentsClient = client.GetFileClient(contentsFilePath);

        CacheEntry entry = CreateEntry();
        await entry.WriteAsync(entryClient, cancellationToken: cancellationToken).ConfigureAwait(false);

        _ = await contentsClient.UploadAsync(
                BinaryData.FromBytes(value).ToStream(),
                overwrite: true, cancellationToken).ConfigureAwait(false);
    }

    internal static async ValueTask ResetStorageAsync(
        DataLakeDirectoryClient client,
        CancellationToken cancellationToken = default)
    {
        _ = await client.DeleteIfExistsAsync(
                recursive: true,
                cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    internal static async ValueTask DeleteExpiredEntriesAsync(
        DataLakeDirectoryClient client,
        Func<DateTime> provideDateTime,
        CancellationToken cancellationToken = default)
    {
        await foreach (PathItem pathItem in
            client.GetPathsAsync(recursive: true, cancellationToken: cancellationToken).ConfigureAwait(false))
        {
            if (pathItem.Name.EndsWith($"/{EntryFileName}", StringComparison.Ordinal))
            {
                DataLakeFileClient entryFileClient = client.GetParentFileSystemClient().GetFileClient(pathItem.Name);

                CacheEntry entry =
                    await CacheEntry.ReadAsync(
                        entryFileClient,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                if (entry.Expiration <= provideDateTime())
                {
                    DataLakeDirectoryClient parentDirectory = entryFileClient.GetParentDirectoryClient();

                    _ = await parentDirectory.DeleteAsync(
                            recursive: true,
                            cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    private (string entryFilePath, string contentsFilePath) GetPaths(string key)
    {
        string entryFilePath = $"{_iterationPath}/{key}/{EntryFileName}";
        string contentsFilePath = $"{_iterationPath}/{key}/{ContentsFileName}";

        return (entryFilePath, contentsFilePath);
    }

    private async ValueTask<(string entryFilePath, string contentsFilePath, bool filesExist)> CheckPathsAsync(
        string key,
        CancellationToken cancellationToken)
    {
        (string entryFilePath, string contentsFilePath) = GetPaths(key);

        DataLakeFileClient entryClient = client.GetFileClient(entryFilePath);
        bool entryFileExists = await entryClient.ExistsAsync(cancellationToken).ConfigureAwait(false);

        DataLakeFileClient contentsClient = client.GetFileClient(contentsFilePath);
        bool contentsFileExists = await contentsClient.ExistsAsync(cancellationToken).ConfigureAwait(false);

        if (entryFileExists == contentsFileExists)
        {
            return (entryFilePath, contentsFilePath, filesExist: contentsFileExists);
        }
        else
        {
            throw new FileNotFoundException(
                contentsFileExists
                    ? string.Format(CultureInfo.CurrentCulture, EntryFileNotFound, entryFilePath)
                    : string.Format(CultureInfo.CurrentCulture, ContentsFileNotFound, contentsFilePath));
        }
    }

    private (string entryFilePath, string contentsFilePath, bool filesExist) CheckPaths(string key)
    {
        (string entryFilePath, string contentsFilePath) = GetPaths(key);

        DataLakeFileClient entryClient = client.GetFileClient(entryFilePath);
        bool entryFileExists = entryClient.Exists();

        DataLakeFileClient contentsClient = client.GetFileClient(contentsFilePath);
        bool contentsFileExists = contentsClient.Exists();

        if (entryFileExists == contentsFileExists)
        {
            return (entryFilePath, contentsFilePath, filesExist: contentsFileExists);
        }
        else
        {
            throw new FileNotFoundException(
                contentsFileExists
                    ? string.Format(CultureInfo.CurrentCulture, EntryFileNotFound, entryFilePath)
                    : string.Format(CultureInfo.CurrentCulture, ContentsFileNotFound, contentsFilePath));
        }
    }

    private CacheEntry CreateEntry()
    {
        DateTime creation = _provideDateTime();
        DateTime expiration = creation.Add(_timeToLiveForCacheEntries);

        return new CacheEntry(scenarioName, iterationName, creation, expiration);
    }
}

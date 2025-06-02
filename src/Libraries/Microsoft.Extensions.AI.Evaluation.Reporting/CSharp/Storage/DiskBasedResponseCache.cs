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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

internal sealed partial class DiskBasedResponseCache : IDistributedCache
{
    private const string EntryFileNotFound = "Cache entry file {0} was not found.";
    private const string ContentsFileNotFound = "Cache contents file {0} was not found.";
    private const string EntryAndContentsFilesNotFound = "Cache entry file {0} and contents file {1} were not found.";

    private readonly string _scenarioName;
    private readonly string _iterationName;

    private readonly string _iterationPath;
    private readonly Func<DateTime> _provideDateTime;
    private readonly TimeSpan _timeToLiveForCacheEntries;

    internal DiskBasedResponseCache(
        string storageRootPath,
        string scenarioName,
        string iterationName,
        Func<DateTime> provideDateTime,
        TimeSpan? timeToLiveForCacheEntries = null)
    {
        _scenarioName = scenarioName;
        _iterationName = iterationName;

        storageRootPath = Path.GetFullPath(storageRootPath);
        string cacheRootPath = GetCacheRootPath(storageRootPath);

        _iterationPath = Path.Combine(cacheRootPath, scenarioName, iterationName);
        _provideDateTime = provideDateTime;
        _timeToLiveForCacheEntries = timeToLiveForCacheEntries ?? Defaults.DefaultTimeToLiveForCacheEntries;
    }

    public byte[]? Get(string key)
    {
        (_, string entryFilePath, string contentsFilePath, bool filesExist) = GetPaths(key);

        if (!filesExist)
        {
            return null;
        }

        CacheEntry entry = CacheEntry.Read(entryFilePath);
        if (entry.Expiration <= _provideDateTime())
        {
            Remove(key);
            return null;
        }

        return File.ReadAllBytes(contentsFilePath);
    }

    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        (string _, string entryFilePath, string contentsFilePath, bool filesExist) = GetPaths(key);

        if (!filesExist)
        {
            return null;
        }

        CacheEntry entry =
            await CacheEntry.ReadAsync(entryFilePath, cancellationToken: cancellationToken).ConfigureAwait(false);

        if (entry.Expiration <= _provideDateTime())
        {
            await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return null;
        }

#if NET
        return await File.ReadAllBytesAsync(contentsFilePath, cancellationToken).ConfigureAwait(false);
#else
        using var stream =
            new FileStream(
                contentsFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

        byte[] buffer = new byte[stream.Length];

        int totalRead = 0;
        while (totalRead < buffer.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int read =
                await stream.ReadAsync(
                    buffer,
                    offset: totalRead,
                    count: buffer.Length - totalRead,
                    cancellationToken).ConfigureAwait(false);

            totalRead += read;

            if (read == 0)
            {
                // End of stream reached.

                if (buffer.Length is not 0 && totalRead != buffer.Length)
                {
                    throw new EndOfStreamException(
                        $"End of stream reached for {contentsFilePath} with {totalRead} bytes read, but {buffer.Length} bytes were expected.");
                }
                else
                {
                    break;
                }
            }
        }

        return buffer;
#endif
    }

    public void Refresh(string key)
    {
        (_, string entryFilePath, string contentsFilePath, bool filesExist) = GetPaths(key);

        if (!filesExist)
        {
            throw new FileNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    EntryAndContentsFilesNotFound,
                    entryFilePath,
                    contentsFilePath));
        }

        CacheEntry entry = CreateEntry();
        entry.Write(entryFilePath);
    }

    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        (_, string entryFilePath, string contentsFilePath, bool filesExist) = GetPaths(key);

        if (!filesExist)
        {
            throw new FileNotFoundException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    EntryAndContentsFilesNotFound,
                    entryFilePath,
                    contentsFilePath));
        }

        CacheEntry entry = CreateEntry();
        await entry.WriteAsync(entryFilePath, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public void Remove(string key)
    {
        (string keyPath, _, _, _) = GetPaths(key);

        Directory.Delete(keyPath, recursive: true);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        Remove(key);

        return Task.CompletedTask;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        (string keyPath, string entryFilePath, string contentsFilePath, _) = GetPaths(key);

        _ = Directory.CreateDirectory(keyPath);

        CacheEntry entry = CreateEntry();
        entry.Write(entryFilePath);

        File.WriteAllBytes(contentsFilePath, value);
    }

    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken cancellationToken = default)
    {
        (string keyPath, string entryFilePath, string contentsFilePath, _) = GetPaths(key);

        Directory.CreateDirectory(keyPath);

        CacheEntry entry = CreateEntry();
        await entry.WriteAsync(entryFilePath, cancellationToken: cancellationToken).ConfigureAwait(false);

#if NET
        await File.WriteAllBytesAsync(contentsFilePath, value, cancellationToken).ConfigureAwait(false);
#else
        using var stream =
            new FileStream(
                contentsFilePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Write,
                bufferSize: 4096,
                useAsync: true);

        await stream.WriteAsync(value, 0, value.Length, cancellationToken).ConfigureAwait(false);
#endif
    }

    internal static void ResetStorage(string storageRootPath)
    {
        string cacheRootPath = GetCacheRootPath(storageRootPath);
        Directory.Delete(cacheRootPath, recursive: true);
        _ = Directory.CreateDirectory(cacheRootPath);
    }

    internal static async ValueTask DeleteExpiredEntriesAsync(
        string storageRootPath,
        Func<DateTime> provideDateTime,
        CancellationToken cancellationToken = default)
    {
        static void DeleteDirectoryIfEmpty(string path)
        {
            if (!Directory.EnumerateFileSystemEntries(path).Any())
            {
                Directory.Delete(path, recursive: true);
            }
        }

        string cacheRootPath = GetCacheRootPath(storageRootPath);

        foreach (string scenarioPath in Directory.GetDirectories(cacheRootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (string iterationPath in Directory.GetDirectories(scenarioPath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (string keyPath in Directory.GetDirectories(iterationPath))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string entryFilePath = GetEntryFilePath(keyPath);

                    CacheEntry entry =
                        await CacheEntry.ReadAsync(
                            entryFilePath,
                            cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (entry.Expiration <= provideDateTime())
                    {
                        Directory.Delete(keyPath, recursive: true);
                    }
                }

                DeleteDirectoryIfEmpty(iterationPath);
            }

            DeleteDirectoryIfEmpty(scenarioPath);
        }
    }

    private static string GetCacheRootPath(string storageRootPath)
        => Path.Combine(storageRootPath, "cache");

    private static string GetEntryFilePath(string keyPath)
        => Path.Combine(keyPath, "entry.json");

    private static string GetContentsFilePath(string keyPath)
        => Path.Combine(keyPath, "contents.data");

    private (string keyPath, string entryFilePath, string contentsFilePath, bool filesExist) GetPaths(string key)
    {
        string keyPath = Path.Combine(_iterationPath, key);
        string entryFilePath = GetEntryFilePath(keyPath);
        string contentsFilePath = GetContentsFilePath(keyPath);

        bool contentsFileExists = File.Exists(contentsFilePath);
        bool entryFileExists = File.Exists(entryFilePath);

        if (entryFileExists == contentsFileExists)
        {
            return (keyPath, entryFilePath, contentsFilePath, filesExist: contentsFileExists);
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

        return new CacheEntry(_scenarioName, _iterationName, creation, expiration);
    }
}

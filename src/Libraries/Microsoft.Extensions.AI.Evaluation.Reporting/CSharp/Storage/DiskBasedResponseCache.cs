// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

/// <summary>
/// An <see cref="IDistributedCache"/> implementation that stores cached AI responses for a particular
/// <see cref="ScenarioRun"/> on disk.
/// </summary>
/// <remarks>
/// <see cref="DiskBasedResponseCache"/> can be used in conjunction with <see cref="ResponseCachingChatClient"/> to
/// implement disk-based caching of all AI responses that happen as part of an evaluation run.
/// </remarks>
public sealed partial class DiskBasedResponseCache : IDistributedCache
{
    private const string EntryFileNotFound = "Cache entry file {0} was not found.";
    private const string ContentsFileNotFound = "Cache contents file {0} was not found.";
    private const string EntryAndContentsFilesNotFound = "Cache entry file {0} and contents file {1} were not found.";

    private readonly string _scenarioName;
    private readonly string _iterationName;

    private readonly CacheOptions _options;
    private readonly string _iterationPath;
    private readonly Func<DateTime> _provideDateTime = () => DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiskBasedResponseCache"/> class.
    /// </summary>
    /// <param name="storageRootPath">
    /// The path to a directory on disk under which the cached AI responses should be stored.
    /// </param>
    /// <param name="scenarioName">
    /// The <see cref="ScenarioRun.ScenarioName"/> for the returned <see cref="DiskBasedResponseCache"/> instance.
    /// </param>
    /// <param name="iterationName">
    /// The <see cref="ScenarioRun.IterationName"/> for the returned <see cref="DiskBasedResponseCache"/> instance.
    /// </param>
    public DiskBasedResponseCache(string storageRootPath, string scenarioName, string iterationName)
    {
        _scenarioName = scenarioName;
        _iterationName = iterationName;

        storageRootPath = Path.GetFullPath(storageRootPath);
        string cacheRootPath = GetCacheRootPath(storageRootPath);
        string optionsFilePath = GetOptionsFilePath(cacheRootPath);
        _options = File.Exists(optionsFilePath) ? CacheOptions.Read(optionsFilePath) : CacheOptions.Default;
        _iterationPath = Path.Combine(cacheRootPath, scenarioName, iterationName);
    }

    /// <remarks>
    /// Intended for testing purposes only.
    /// </remarks>
    internal DiskBasedResponseCache(string storageRootPath, string scenarioName, string iterationName, Func<DateTime> timeProvider)
        : this(storageRootPath, scenarioName, iterationName)
    {
        _provideDateTime = timeProvider;
    }

    /// <inheritdoc/>
    public byte[]? Get(string key)
    {
        if (_options.Mode is CacheMode.Disabled)
        {
            return null;
        }

        (_, string entryFilePath, string contentsFilePath, bool filesExist) = GetPaths(key);
        if (!filesExist)
        {
            return _options.Mode is CacheMode.EnabledOfflineOnly
                ? throw new FileNotFoundException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        EntryAndContentsFilesNotFound,
                        entryFilePath,
                        contentsFilePath))
                : null;
        }

        if (_options.Mode is not CacheMode.EnabledOfflineOnly)
        {
            CacheEntry entry = CacheEntry.Read(entryFilePath);
            if (entry.Expiration <= _provideDateTime())
            {
                Remove(key);
                return null;
            }
        }

        return File.ReadAllBytes(contentsFilePath);
    }

    /// <inheritdoc/>
    public async Task<byte[]?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_options.Mode is CacheMode.Disabled)
        {
            return null;
        }

        (string _, string entryFilePath, string contentsFilePath, bool filesExist) = GetPaths(key);
        if (!filesExist)
        {
            return _options.Mode is CacheMode.EnabledOfflineOnly
                ? throw new FileNotFoundException(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        EntryAndContentsFilesNotFound,
                        entryFilePath,
                        contentsFilePath))
                : null;
        }

        if (_options.Mode is not CacheMode.EnabledOfflineOnly)
        {
            CacheEntry entry =
                await CacheEntry.ReadAsync(entryFilePath, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (entry.Expiration <= _provideDateTime())
            {
                await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                return null;
            }
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

    /// <inheritdoc/>
    public void Refresh(string key)
    {
        if (_options.Mode is CacheMode.Disabled or CacheMode.EnabledOfflineOnly)
        {
            return;
        }

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

    /// <inheritdoc/>
    public async Task RefreshAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_options.Mode is CacheMode.Disabled or CacheMode.EnabledOfflineOnly)
        {
            return;
        }

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

    /// <inheritdoc/>
    public void Remove(string key)
    {
        if (_options.Mode is CacheMode.Disabled or CacheMode.EnabledOfflineOnly)
        {
            return;
        }

        (string keyPath, _, _, _) = GetPaths(key);
        Directory.Delete(keyPath, recursive: true);
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_options.Mode is CacheMode.Disabled or CacheMode.EnabledOfflineOnly)
        {
            return Task.CompletedTask;
        }

        Remove(key);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        if (_options.Mode is CacheMode.Disabled or CacheMode.EnabledOfflineOnly)
        {
            return;
        }

        (string keyPath, string entryFilePath, string contentsFilePath, _) = GetPaths(key);

        _ = Directory.CreateDirectory(keyPath);

        CacheEntry entry = CreateEntry();
        entry.Write(entryFilePath);

        File.WriteAllBytes(contentsFilePath, value);
    }

    /// <inheritdoc/>
    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken cancellationToken = default)
    {
        if (_options.Mode is CacheMode.Disabled or CacheMode.EnabledOfflineOnly)
        {
            return;
        }

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

    private static string GetOptionsFilePath(string cacheRootPath)
        => Path.Combine(cacheRootPath, "options.json");

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
        DateTime expiration = creation.Add(_options.TimeToLiveForCacheEntries);

        return new CacheEntry(_scenarioName, _iterationName, creation, expiration);
    }
}

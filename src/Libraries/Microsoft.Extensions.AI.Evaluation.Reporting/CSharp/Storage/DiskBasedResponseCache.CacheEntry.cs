// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

public partial class DiskBasedResponseCache
{
    [method: JsonConstructor]
    internal sealed class CacheEntry(
        string scenarioName,
        string iterationName,
        DateTime creation,
        DateTime expiration)
    {
        private const string DeserializationFailedMessage = "Unable to deserialize the cache entry file at {0}.";

        public string ScenarioName { get; } = scenarioName;
        public string IterationName { get; } = iterationName;
        public DateTime Creation { get; } = creation;
        public DateTime Expiration { get; } = expiration;

        public static CacheEntry Read(string cacheEntryFilePath)
        {
            using FileStream cacheEntryFile = File.OpenRead(cacheEntryFilePath);

            CacheEntry cacheEntry =
                JsonSerializer.Deserialize(
                    cacheEntryFile,
                    SerializerContext.Default.CacheEntry) ??
                throw new JsonException(
                    string.Format(CultureInfo.CurrentCulture, DeserializationFailedMessage, cacheEntryFilePath));

            return cacheEntry;
        }

        public static async Task<CacheEntry> ReadAsync(
            string cacheEntryFilePath,
            CancellationToken cancellationToken = default)
        {
            using FileStream cacheEntryFile = File.OpenRead(cacheEntryFilePath);

            CacheEntry cacheEntry =
                await JsonSerializer.DeserializeAsync(
                    cacheEntryFile,
                    SerializerContext.Default.CacheEntry,
                    cancellationToken).ConfigureAwait(false) ??
                throw new JsonException(
                    string.Format(CultureInfo.CurrentCulture, DeserializationFailedMessage, cacheEntryFilePath));

            return cacheEntry;
        }

        public void Write(string cacheEntryFilePath)
        {
            using FileStream cacheEntryFile = File.Create(cacheEntryFilePath);
            JsonSerializer.Serialize(cacheEntryFile, this, SerializerContext.Default.CacheEntry);
        }

        public async Task WriteAsync(
            string cacheEntryFilePath,
            CancellationToken cancellationToken = default)
        {
            using FileStream cacheEntryFile = File.Create(cacheEntryFilePath);
            await JsonSerializer.SerializeAsync(
                cacheEntryFile,
                this,
                SerializerContext.Default.CacheEntry,
                cancellationToken).ConfigureAwait(false);
        }
    }
}

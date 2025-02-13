// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal sealed class CacheOptions
    {
        public static CacheOptions Default { get; } = new CacheOptions();

        private const string DeserializationFailedMessage = "Unable to deserialize the cache options file at {0}.";

        public CacheOptions(CacheMode mode = CacheMode.Enabled, TimeSpan? timeToLiveForCacheEntries = null)
        {
            Mode = mode;
            TimeToLiveForCacheEntries = timeToLiveForCacheEntries ?? Defaults.DefaultTimeToLiveForCacheEntries;
        }

        [JsonConstructor]
        public CacheOptions(CacheMode mode, TimeSpan timeToLiveForCacheEntries)
        {
            Mode = mode;
            TimeToLiveForCacheEntries = timeToLiveForCacheEntries;
        }

        public CacheMode Mode { get; }

        [JsonPropertyName("timeToLiveInSecondsForCacheEntries")]
        public TimeSpan TimeToLiveForCacheEntries { get; }

        public static CacheOptions Read(string cacheOptionsFilePath)
        {
            using FileStream cacheOptionsFile = File.OpenRead(cacheOptionsFilePath);

            CacheOptions cacheOptions =
                JsonSerializer.Deserialize(
                    cacheOptionsFile,
                    SerializerContext.Default.CacheOptions) ??
                throw new JsonException(
                    string.Format(CultureInfo.CurrentCulture, DeserializationFailedMessage, cacheOptionsFilePath));

            return cacheOptions;
        }

        public static async Task<CacheOptions> ReadAsync(
            string cacheOptionsFilePath,
            CancellationToken cancellationToken = default)
        {
            using FileStream cacheOptionsFile = File.OpenRead(cacheOptionsFilePath);

            CacheOptions cacheOptions =
                await JsonSerializer.DeserializeAsync<CacheOptions>(
                    cacheOptionsFile,
                    SerializerContext.Default.CacheOptions,
                    cancellationToken).ConfigureAwait(false) ??
                throw new JsonException(
                    string.Format(CultureInfo.CurrentCulture, DeserializationFailedMessage, cacheOptionsFilePath));

            return cacheOptions;
        }

        public void Write(string cacheOptionsFilePath)
        {
            using FileStream cacheOptionsFile = File.Create(cacheOptionsFilePath);
            JsonSerializer.Serialize(cacheOptionsFile, this, SerializerContext.Default.CacheOptions);
        }

        public async Task WriteAsync(
            string cacheOptionsFilePath,
            CancellationToken cancellationToken = default)
        {
            using FileStream cacheOptionsFile = File.Create(cacheOptionsFilePath);
            await JsonSerializer.SerializeAsync(
                cacheOptionsFile,
                this,
                SerializerContext.Default.CacheOptions,
                cancellationToken).ConfigureAwait(false);
        }
    }
}

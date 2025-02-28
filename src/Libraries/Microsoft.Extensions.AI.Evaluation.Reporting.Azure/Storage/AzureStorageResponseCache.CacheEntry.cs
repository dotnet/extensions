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
using Azure;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Storage;

public partial class AzureStorageResponseCache
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

        public static CacheEntry Read(
            DataLakeFileClient fileClient,
            CancellationToken cancellationToken = default)
        {
            Response<DataLakeFileReadResult> content = fileClient.ReadContent(cancellationToken);

            CacheEntry cacheEntry =
                JsonSerializer.Deserialize(
                    content.Value.Content.ToMemory().Span,
                    AzureStorageSerializerContext.Default.CacheEntry)
                ?? throw new JsonException(
                    string.Format(CultureInfo.CurrentCulture, DeserializationFailedMessage, fileClient.Name));

            return cacheEntry;
        }

        public static async Task<CacheEntry> ReadAsync(
            DataLakeFileClient fileClient,
            CancellationToken cancellationToken = default)
        {
            Response<DataLakeFileReadResult> content =
                await fileClient.ReadContentAsync(cancellationToken).ConfigureAwait(false);

            CacheEntry cacheEntry =
                await JsonSerializer.DeserializeAsync(
                    content.Value.Content.ToStream(),
                    AzureStorageSerializerContext.Default.CacheEntry,
                    cancellationToken).ConfigureAwait(false)
                ?? throw new JsonException(
                    string.Format(CultureInfo.CurrentCulture, DeserializationFailedMessage, fileClient.Name));

            return cacheEntry;
        }

        public void Write(
            DataLakeFileClient fileClient,
            CancellationToken cancellationToken = default)
        {
            MemoryStream stream = new();

            JsonSerializer.Serialize(stream, this, AzureStorageSerializerContext.Default.CacheEntry);

            _ = stream.Seek(0, SeekOrigin.Begin);
            _ = fileClient.Upload(stream, overwrite: true, cancellationToken);
        }

        public async Task WriteAsync(
            DataLakeFileClient fileClient,
            CancellationToken cancellationToken = default)
        {
            MemoryStream stream = new();

            await JsonSerializer.SerializeAsync(
                stream,
                this,
                AzureStorageSerializerContext.Default.CacheEntry,
                cancellationToken).ConfigureAwait(false);

            _ = stream.Seek(0, SeekOrigin.Begin);
            _ = await fileClient.UploadAsync(stream, overwrite: true, cancellationToken).ConfigureAwait(false);
        }
    }
}

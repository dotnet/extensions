// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.VectorData;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Writes chunks to a <see cref="VectorStoreCollection{TKey, TRecord}"/>.
/// </summary>
/// <typeparam name="TChunk">The type of the chunk content.</typeparam>
/// <typeparam name="TRecord">The type of the record stored in the vector store.</typeparam>
public class VectorStoreWriter<TChunk, TRecord> : IngestionChunkWriter<TChunk>
    where TRecord : IngestedChunkRecord<TChunk>, new()
{
    private readonly VectorStoreWriterOptions _options;
    private bool _collectionEnsured;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreWriter{TChunk, TRecord}"/> class.
    /// </summary>
    /// <param name="collection">The <see cref="VectorStoreCollection{TKey, TRecord}"/> to use to store the <see cref="IngestionChunk{T}"/> instances.</param>
    /// <param name="options">The options for the vector store writer.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="collection"/> is null.</exception>
    public VectorStoreWriter(VectorStoreCollection<Guid, TRecord> collection, VectorStoreWriterOptions? options = default)
    {
        VectorStoreCollection = Throw.IfNull(collection);
        _options = options ?? new VectorStoreWriterOptions();
    }

    /// <summary>
    /// Gets the underlying <see cref="VectorStoreCollection{TKey,TRecord}"/> used to store the chunks.
    /// </summary>
    public VectorStoreCollection<Guid, TRecord> VectorStoreCollection { get; }

    /// <inheritdoc/>
    public override async Task WriteAsync(IAsyncEnumerable<IngestionChunk<TChunk>> chunks, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chunks);

        IReadOnlyList<Guid>? preExistingKeys = null;
        List<TRecord>? batch = null;
        long currentBatchTokenCount = 0;

        await foreach (IngestionChunk<TChunk> chunk in chunks.WithCancellation(cancellationToken))
        {
            if (!_collectionEnsured)
            {
                await VectorStoreCollection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);
                _collectionEnsured = true;
            }

            // We obtain the IDs of the pre-existing chunks for given document,
            // and delete them after we finish inserting the new chunks,
            // to avoid a situation where we delete the chunks and then fail to insert the new ones.
            preExistingKeys ??= await GetPreExistingChunksIdsAsync(chunk.Document, cancellationToken).ConfigureAwait(false);

            TRecord record = new()
            {
                Key = Guid.NewGuid(),
                Content = chunk.Content,
                Context = chunk.Context,
                DocumentId = chunk.Document.Identifier,
            };

            if (chunk.HasMetadata)
            {
                foreach (var metadata in chunk.Metadata)
                {
                    SetMetadata(record, metadata.Key, metadata.Value);
                }
            }

            batch ??= [];

            // Check if adding this chunk would exceed the batch token limit
            // If the batch is empty or the chunk alone exceeds the limit, add it anyway.
            if (batch.Count > 0 && currentBatchTokenCount + chunk.TokenCount > _options.BatchTokenCount)
            {
                await VectorStoreCollection.UpsertAsync(batch, cancellationToken).ConfigureAwait(false);

                batch.Clear();
                currentBatchTokenCount = 0;
            }

            batch.Add(record);
            currentBatchTokenCount += chunk.TokenCount;
        }

        // Upsert any remaining chunks in the batch
        if (batch?.Count > 0)
        {
            await VectorStoreCollection.UpsertAsync(batch, cancellationToken).ConfigureAwait(false);
        }

        if (preExistingKeys?.Count > 0)
        {
            await VectorStoreCollection.DeleteAsync(preExistingKeys, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sets a metadata value on the record.
    /// </summary>
    /// <param name="record">The record on which to set the metadata.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <remarks>
    /// Override this method in derived classes to store metadata as typed properties with
    /// <see cref="VectorStoreDataAttribute"/> attributes.
    /// </remarks>
    protected virtual void SetMetadata(TRecord record, string key, object? value)
    {
        throw new NotSupportedException($"Metadata key '{key}' is not supported. Override {nameof(SetMetadata)} in a derived class to handle metadata.");
    }

    private async Task<IReadOnlyList<Guid>> GetPreExistingChunksIdsAsync(IngestionDocument document, CancellationToken cancellationToken)
    {
        if (!_options.IncrementalIngestion)
        {
            return [];
        }

        // Each Vector Store has a different max top count limit, so we use low value and loop.
        const int MaxTopCount = 1_000;

        List<Guid> keys = [];
        int insertedCount;
        do
        {
            insertedCount = 0;

            await foreach (var record in VectorStoreCollection.GetAsync(
                filter: record => record.DocumentId == document.Identifier,
                top: MaxTopCount,
                options: new() { Skip = keys.Count },
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                keys.Add(record.Key);
                insertedCount++;
            }
        }
        while (insertedCount == MaxTopCount);

        return keys;
    }
}

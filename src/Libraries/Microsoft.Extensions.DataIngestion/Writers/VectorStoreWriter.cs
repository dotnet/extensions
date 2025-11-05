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
/// Writes chunks to the <see cref="VectorStore"/> using the default schema. 
/// </summary>
/// <typeparam name="T">The type of the chunk content.</typeparam>
public sealed class VectorStoreWriter<T> : IngestionChunkWriter<T>
{
    // The names are lowercase with no special characters to ensure compatibility with various vector stores.
    private const string KeyName = "key";
    private const string EmbeddingName = "embedding";
    private const string ContentName = "content";
    private const string ContextName = "context";
    private const string DocumentIdName = "documentid";

    private readonly VectorStore _vectorStore;
    private readonly int _dimensionCount;
    private readonly VectorStoreWriterOptions _options;

    private VectorStoreCollection<object, Dictionary<string, object?>>? _vectorStoreCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="VectorStoreWriter{T}"/> class.
    /// </summary>
    /// <param name="vectorStore">The <see cref="VectorStore"/> to use to store the <see cref="IngestionChunk{T}"/> instances.</param>
    /// <param name="dimensionCount">The number of dimensions that the vector has. This value is required when creating collections.</param>
    /// <param name="options">The options for the vector store writer.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="vectorStore"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="dimensionCount"/> is less or equal zero.</exception>
    public VectorStoreWriter(VectorStore vectorStore, int dimensionCount, VectorStoreWriterOptions? options = default)
    {
        _vectorStore = Throw.IfNull(vectorStore);
        _dimensionCount = Throw.IfLessThanOrEqual(dimensionCount, 0);
        _options = options ?? new VectorStoreWriterOptions();
    }

    /// <summary>
    /// Gets the underlying <see cref="VectorStoreCollection{TKey,TRecord}"/> used to store the chunks.
    /// </summary>
    /// <remarks>
    /// The collection is initialized when <see cref="WriteAsync(IAsyncEnumerable{IngestionChunk{T}}, CancellationToken)"/> is called for the first time.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The collection has not been initialized yet.
    /// Call <see cref="WriteAsync(IAsyncEnumerable{IngestionChunk{T}}, CancellationToken)"/> first.</exception>
    public VectorStoreCollection<object, Dictionary<string, object?>> VectorStoreCollection
        => _vectorStoreCollection ?? throw new InvalidOperationException("The collection has not been initialized yet. Call WriteAsync first.");

    /// <inheritdoc/>
    public override async Task WriteAsync(IAsyncEnumerable<IngestionChunk<T>> chunks, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(chunks);

        IReadOnlyList<object>? preExistingKeys = null;
        await foreach (IngestionChunk<T> chunk in chunks.WithCancellation(cancellationToken))
        {
            if (_vectorStoreCollection is null)
            {
                _vectorStoreCollection = _vectorStore.GetDynamicCollection(_options.CollectionName, GetVectorStoreRecordDefinition(chunk));

                await _vectorStoreCollection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);
            }

            // We obtain the IDs of the pre-existing chunks for given document,
            // and delete them after we finish inserting the new chunks,
            // to avoid a situation where we delete the chunks and then fail to insert the new ones.
            preExistingKeys ??= await GetPreExistingChunksIdsAsync(chunk.Document, cancellationToken).ConfigureAwait(false);

            var key = Guid.NewGuid();
            Dictionary<string, object?> record = new()
            {
                [KeyName] = key,
                [ContentName] = chunk.Content,
                [EmbeddingName] = chunk.Content,
                [ContextName] = chunk.Context,
                [DocumentIdName] = chunk.Document.Identifier,
            };

            if (chunk.HasMetadata)
            {
                foreach (var metadata in chunk.Metadata)
                {
                    record[metadata.Key] = metadata.Value;
                }
            }

            await _vectorStoreCollection.UpsertAsync(record, cancellationToken).ConfigureAwait(false);
        }

        if (preExistingKeys?.Count > 0)
        {
            await _vectorStoreCollection!.DeleteAsync(preExistingKeys, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        try
        {
            _vectorStoreCollection?.Dispose();
        }
        finally
        {
            _vectorStore.Dispose();
            base.Dispose(disposing);
        }
    }

    private VectorStoreCollectionDefinition GetVectorStoreRecordDefinition(IngestionChunk<T> representativeChunk)
    {
        VectorStoreCollectionDefinition definition = new()
        {
            Properties =
            {
                new VectorStoreKeyProperty(KeyName, typeof(Guid)),

                // By using T as the type here we allow the vector store
                // to handle the conversion from T to the actual vector type it supports.
                new VectorStoreVectorProperty(EmbeddingName, typeof(T), _dimensionCount)
                {
                    DistanceFunction = _options.DistanceFunction,
                    IndexKind = _options.IndexKind
                },
                new VectorStoreDataProperty(ContentName, typeof(T)),
                new VectorStoreDataProperty(ContextName, typeof(string)),
                new VectorStoreDataProperty(DocumentIdName, typeof(string))
                {
                    IsIndexed = true
                }
            }
        };

        if (representativeChunk.HasMetadata)
        {
            foreach (var metadata in representativeChunk.Metadata)
            {
                Type propertyType = metadata.Value.GetType();
                definition.Properties.Add(new VectorStoreDataProperty(metadata.Key, propertyType)
                {
                    // We use lowercase storage names to ensure compatibility with various vector stores.
#pragma warning disable CA1308 // Normalize strings to uppercase
                    StorageName = metadata.Key.ToLowerInvariant()
#pragma warning restore CA1308 // Normalize strings to uppercase

                    // We could consider indexing for certain keys like classification etc. but for now we leave it as non-indexed.
                    // The reason is that not every DB supports it, moreover we would need to expose the ability to configure it.
                });
            }
        }

        return definition;
    }

    private async Task<IReadOnlyList<object>> GetPreExistingChunksIdsAsync(IngestionDocument document, CancellationToken cancellationToken)
    {
        if (!_options.IncrementalIngestion)
        {
            return [];
        }

        // Each Vector Store has a different max top count limit, so we use low value and loop.
        const int MaxTopCount = 1_000;

        List<object> keys = [];
        int insertedCount;
        do
        {
            insertedCount = 0;

            await foreach (var record in _vectorStoreCollection!.GetAsync(
                filter: record => (string)record[DocumentIdName]! == document.Identifier,
                top: MaxTopCount,
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                keys.Add(record[KeyName]!);
                insertedCount++;
            }
        }
        while (insertedCount == MaxTopCount);

        return keys;
    }
}

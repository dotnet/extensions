// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.VectorData;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DataIngestion;

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
    private readonly bool _keysAreStrings;

    private VectorStoreCollection<object, Dictionary<string, object?>>? _vectorStoreCollection;

    /// <summary>
    /// Creates a new instance of <see cref="VectorStoreCollection{TKey, Dictionary{string, object?}}"/> that uses dynamic schema to store the <see cref="IngestionChunk"/> instances as <see cref="Dictionary{string, object?}"/> using provided vector store, collection name and dimension count.
    /// </summary>
    /// <param name="vectorStore">The <see cref="VectorStore"/> to use to store the <see cref="IngestionChunk"/> instances.</param>
    /// <param name="dimensionCount">The number of dimensions that the vector has. This value is required when creating collections.</param>
    /// <param name="options">The options for the vector store writer.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="vectorStore"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="dimensionCount"/> is less or equal zero.</exception>
    public VectorStoreWriter(VectorStore vectorStore, int dimensionCount, VectorStoreWriterOptions? options = default)
    {
        _vectorStore = vectorStore ?? throw new ArgumentNullException(nameof(vectorStore));
        _dimensionCount = dimensionCount > 0 ? dimensionCount : throw new ArgumentOutOfRangeException(nameof(dimensionCount));
        _options = options ?? new VectorStoreWriterOptions();
        // Not all vector store support string as the key type, examples:
        // Qdrant: https://github.com/microsoft/semantic-kernel/blob/28ea2f4df872e8fd03ef0792ebc9e1989b4be0ee/dotnet/src/VectorData/Qdrant/QdrantCollection.cs#L104
        // When https://github.com/microsoft/semantic-kernel/issues/13141 gets released,
        // we are going to support Guid keys as well.
        _keysAreStrings = vectorStore.GetType().Name != "QdrantVectorStore";
    }

    public VectorStoreCollection<object, Dictionary<string, object?>> VectorStoreCollection
        => _vectorStoreCollection ?? throw new InvalidOperationException("The collection has not been initialized yet. Call WriteAsync first.");

    protected override void Dispose(bool disposing)
    {
        _vectorStore.Dispose();
        _vectorStoreCollection?.Dispose();
    }

    public override async Task WriteAsync(IAsyncEnumerable<IngestionChunk<T>> chunks, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (chunks is null)
        {
            throw new ArgumentNullException(nameof(chunks));
        }

        IReadOnlyList<object>? preExistingKeys = null;
        await foreach (IngestionChunk<T> chunk in chunks.WithCancellation(cancellationToken))
        {
            if (_vectorStoreCollection is null)
            {
                _vectorStoreCollection = _vectorStore.GetDynamicCollection(_options.CollectionName, GetVectorStoreRecordDefinition(chunk));

                await _vectorStoreCollection.EnsureCollectionExistsAsync(cancellationToken).ConfigureAwait(false);
            }

            if (preExistingKeys is null)
            {
                // We obtain the IDs of the pre-existing chunks for given document,
                // and delete them after we finish inserting the new chunks.
                // To avoid a situation where we delete the chunks and then fail to insert the new ones.
                preExistingKeys = await GetPreExistingChunksIds(chunk.Document, cancellationToken).ConfigureAwait(false);
            }

            Guid key = Guid.NewGuid();
            Dictionary<string, object?> record = new()
            {
                [KeyName] = _keysAreStrings ? key.ToString() : key,
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

    private VectorStoreCollectionDefinition GetVectorStoreRecordDefinition(IngestionChunk<T> representativeChunk)
    {
        VectorStoreCollectionDefinition definition = new()
        {
            Properties =
            {
                new VectorStoreKeyProperty(KeyName, _keysAreStrings ? typeof(string) : typeof(Guid)),
                // By using T as the type here we allow the vector store
                // to handle the conversion from T to the actual vector type it supports.
                new VectorStoreVectorProperty(EmbeddingName, typeof(T), _dimensionCount)
                {
                    DistanceFunction = _options.DistanceFunction
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
                    StorageName = metadata.Key.ToLowerInvariant()
                    // We could consider indexing for certain keys like classification etc. but for now we leave it as non-indexed.
                    // The reason is that not every DB supports it, moreover we would need to expose the ability to configure it.
                });
            }
        }

        return definition;
    }

    private async Task<IReadOnlyList<object>> GetPreExistingChunksIds(IngestionDocument document, CancellationToken cancellationToken)
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
                cancellationToken: cancellationToken))
            {
                keys.Add(record[KeyName]!);
                insertedCount++;
            }
        } while (insertedCount == MaxTopCount);

        return keys;
    }
}

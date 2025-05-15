using Microsoft.Extensions.VectorData;
using System.Linq.Expressions;
using System.Numerics.Tensors;
using System.Reflection;
using System.Text.Json;

namespace ChatWithCustomData_CSharp.Web.Services;

/// <summary>
/// This IVectorStore implementation is for prototyping only. Do not use this in production.
/// In production, you must replace this with a real vector store. There are many IVectorStore
/// implementations available, including ones for standalone vector databases like Qdrant or Milvus,
/// or for integrating with relational databases such as SQL Server or PostgreSQL.
///
/// This implementation stores the vector records in large JSON files on disk. It is very inefficient
/// and is provided only for convenience when prototyping.
/// </summary>
public class JsonVectorStore(string basePath) : IVectorStore
{
    public Task<bool> CollectionExistsAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult(File.Exists(FilePath(name)));

    public Task DeleteCollectionAsync(string name, CancellationToken cancellationToken = default)
    {
        File.Delete(FilePath(name));
        return Task.CompletedTask;
    }

    public IVectorStoreRecordCollection<TKey, TRecord> GetCollection<TKey, TRecord>(string name, VectorStoreRecordDefinition? vectorStoreRecordDefinition = null)
        where TKey : notnull
        where TRecord : notnull
        => new JsonVectorStoreRecordCollection<TKey, TRecord>(name, FilePath(name), vectorStoreRecordDefinition);

    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceKey is not null ? null :
            serviceType.IsInstanceOfType(this) ? this :
            null;

    public IAsyncEnumerable<string> ListCollectionNamesAsync(CancellationToken cancellationToken = default)
        => Directory.EnumerateFiles(basePath, "*.json").Select(f => Path.GetFileNameWithoutExtension(f)!).ToAsyncEnumerable();

    private string FilePath(string collectionName)
        => Path.Combine(basePath, collectionName + ".json");

    private class JsonVectorStoreRecordCollection<TKey, TRecord> : IVectorStoreRecordCollection<TKey, TRecord>
        where TKey : notnull
        where TRecord : notnull
    {
        private static readonly Func<TRecord, TKey> _getKey = CreateKeyReader();
        private static readonly Func<TRecord, ReadOnlyMemory<float>> _getVector = CreateVectorReader();

        private readonly string _name;
        private readonly string _filePath;
        private Dictionary<TKey, TRecord>? _records;

        public JsonVectorStoreRecordCollection(string name, string filePath, VectorStoreRecordDefinition? vectorStoreRecordDefinition)
        {
            _name = name;
            _filePath = filePath;

            if (File.Exists(filePath))
            {
                _records = JsonSerializer.Deserialize<Dictionary<TKey, TRecord>>(File.ReadAllText(filePath));
            }
        }

        public string Name => _name;

        public Task<bool> CollectionExistsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_records is not null);

        public async Task CreateCollectionAsync(CancellationToken cancellationToken = default)
        {
            _records = [];
            await WriteToDiskAsync(cancellationToken);
        }

        public async Task CreateCollectionIfNotExistsAsync(CancellationToken cancellationToken = default)
        {
            if (_records is null)
            {
                await CreateCollectionAsync(cancellationToken);
            }
        }

        public Task DeleteAsync(TKey key, CancellationToken cancellationToken = default)
        {
            _records!.Remove(key);
            return WriteToDiskAsync(cancellationToken);
        }

        public Task DeleteAsync(IEnumerable<TKey> keys, CancellationToken cancellationToken = default)
        {
            foreach (var key in keys)
            {
                _records!.Remove(key);
            }

            return WriteToDiskAsync(cancellationToken);
        }

        public Task DeleteCollectionAsync(CancellationToken cancellationToken = default)
        {
            _records = null;
            File.Delete(_filePath);
            return Task.CompletedTask;
        }

        public Task<TRecord?> GetAsync(TKey key, GetRecordOptions? options = null, CancellationToken cancellationToken = default)
            => Task.FromResult(_records!.GetValueOrDefault(key));

        public IAsyncEnumerable<TRecord> GetAsync(IEnumerable<TKey> keys, GetRecordOptions? options = null, CancellationToken cancellationToken = default)
            => keys.Select(key => _records!.GetValueOrDefault(key)!).Where(r => r is not null).ToAsyncEnumerable();

        public IAsyncEnumerable<TRecord> GetAsync(Expression<Func<TRecord, bool>> filter, int top, GetFilteredRecordOptions<TRecord>? options = null, CancellationToken cancellationToken = default)
        {
            var filterCompiled = filter.Compile();
            var matches = _records!.Values.Where(r => filterCompiled(r));

            if (options?.OrderBy is { } orderBy)
            {
                var matchesQueryable = matches.AsQueryable();
                foreach (var sort in orderBy.Values)
                {
                    matchesQueryable = sort.Ascending ? matchesQueryable.OrderBy(sort.PropertySelector) : matchesQueryable.OrderByDescending(sort.PropertySelector);
                }
                matches = matchesQueryable;
            }

            return matches.Take(top).Skip(options?.Skip ?? 0).ToAsyncEnumerable();
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => null;

        public IAsyncEnumerable<VectorSearchResult<TRecord>> SearchAsync<TInput>(TInput value, int top, VectorSearchOptions<TRecord>? options = null, CancellationToken cancellationToken = default) where TInput : notnull
        {
            throw new NotImplementedException("The temporary JsonVectorStore type does not support generating embeddings. Use SearchEmbeddingAsync instead.");
        }

        public IAsyncEnumerable<VectorSearchResult<TRecord>> SearchEmbeddingAsync<TVector>(TVector vector, int top, VectorSearchOptions<TRecord>? options = null, CancellationToken cancellationToken = default) where TVector : notnull
        {
            if (vector is not ReadOnlyMemory<float> floatVector)
            {
                throw new NotSupportedException($"The provided vector type {vector!.GetType().FullName} is not supported.");
            }

            IEnumerable<TRecord> filteredRecords = _records!.Values;
            if (options?.Filter is { } filter)
            {
                filteredRecords = filteredRecords.AsQueryable().Where(filter);
            }

            var ranked = from record in filteredRecords
                         let candidateVector = _getVector(record)
                         let similarity = TensorPrimitives.CosineSimilarity(candidateVector.Span, floatVector.Span)
                         orderby similarity descending
                         select (Record: record, Similarity: similarity);

            var results = ranked.Skip(options?.Skip ?? 0).Take(top).Select(r => new VectorSearchResult<TRecord>(r.Record, r.Similarity));
            return results.ToAsyncEnumerable();
        }

        public IAsyncEnumerable<VectorSearchResult<TRecord>> VectorizedSearchAsync<TVector>(TVector vector, int top, VectorSearchOptions<TRecord>? options = null, CancellationToken cancellationToken = default) where TVector : notnull
            => SearchEmbeddingAsync(vector, top, options, cancellationToken);

        public async Task<TKey> UpsertAsync(TRecord record, CancellationToken cancellationToken = default)
        {
            var key = _getKey(record);
            _records![key] = record;
            await WriteToDiskAsync(cancellationToken);
            return key;
        }

        public async Task<IReadOnlyList<TKey>> UpsertAsync(IEnumerable<TRecord> records, CancellationToken cancellationToken = default)
        {
            var results = new List<TKey>();
            foreach (var record in records)
            {
                var key = _getKey(record);
                _records![key] = record;
                results.Add(key);
            }

            await WriteToDiskAsync(cancellationToken);
            return results;
        }

        private async Task WriteToDiskAsync(CancellationToken cancellationToken = default)
        {
            var json = JsonSerializer.Serialize(_records);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            await File.WriteAllTextAsync(_filePath, json, cancellationToken);
        }

        private static Func<TRecord, TKey> CreateKeyReader()
        {
            var propertyInfo = typeof(TRecord).GetProperties()
                .Where(p => p.GetCustomAttribute<VectorStoreRecordKeyAttribute>() is not null
                    && p.PropertyType == typeof(TKey))
                .Single();
            return record => (TKey)propertyInfo.GetValue(record)!;
        }

        private static Func<TRecord, ReadOnlyMemory<float>> CreateVectorReader()
        {
            var propertyInfo = typeof(TRecord).GetProperties()
                .Where(p => p.GetCustomAttribute<VectorStoreRecordVectorAttribute>() is not null
                    && p.PropertyType == typeof(ReadOnlyMemory<float>))
                .Single();
            return record => (ReadOnlyMemory<float>)propertyInfo.GetValue(record)!;
        }
    }
}

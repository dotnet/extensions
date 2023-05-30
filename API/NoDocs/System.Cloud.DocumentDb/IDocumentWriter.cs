// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.DocumentDb;

public interface IDocumentWriter<TDocument> where TDocument : notnull
{
    Task<IDatabaseResponse<TDocument>> PatchDocumentAsync(RequestOptions<TDocument> options, string id, IReadOnlyList<PatchOperation> patchOperations, string? filter, CancellationToken cancellationToken);
    Task<IDatabaseResponse<TDocument>> CreateDocumentAsync(RequestOptions<TDocument> options, CancellationToken cancellationToken);
    Task<IDatabaseResponse<TDocument>> ReplaceDocumentAsync(RequestOptions<TDocument> options, string id, CancellationToken cancellationToken);
    Task<IDatabaseResponse<TDocument>> InsertOrUpdateDocumentAsync(RequestOptions<TDocument> options, string id, Func<TDocument, TDocument> conflictResolveFunc, CancellationToken cancellationToken);
    Task<IDatabaseResponse<TDocument>> UpsertDocumentAsync(RequestOptions<TDocument> options, CancellationToken cancellationToken);
    Task<IDatabaseResponse<bool>> DeleteDocumentAsync(RequestOptions<TDocument> options, string id, CancellationToken cancellationToken);
    Task<IDatabaseResponse<IReadOnlyList<IDatabaseResponse<TDocument>>>> ExecuteTransactionalBatchAsync(RequestOptions<TDocument> requestOptions, IReadOnlyList<BatchItem<TDocument>> itemsToPerformTransactionalBatch, CancellationToken cancellationToken);
}

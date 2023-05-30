// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.DocumentDb;

public interface IDocumentReader<TDocument> where TDocument : notnull
{
    Task<IDatabaseResponse<TDocument>> ReadDocumentAsync(RequestOptions<TDocument> requestOptions, string id, CancellationToken cancellationToken);
    Task<IDatabaseResponse<IReadOnlyList<TOutputDocument>>> FetchDocumentsAsync<TOutputDocument>(QueryRequestOptions<TDocument> options, Func<IQueryable<TDocument>, IQueryable<TOutputDocument>>? condition, CancellationToken cancellationToken) where TOutputDocument : notnull;
    Task<IDatabaseResponse<IReadOnlyList<TDocument>>> QueryDocumentsAsync(QueryRequestOptions<TDocument> options, Query query, CancellationToken cancellationToken);
    Task<IDatabaseResponse<int>> CountDocumentsAsync(QueryRequestOptions<TDocument> options, Func<IQueryable<TDocument>, IQueryable<TDocument>>? condition, CancellationToken cancellationToken);
}

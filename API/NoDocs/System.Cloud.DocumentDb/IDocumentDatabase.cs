// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Threading;
using System.Threading.Tasks;

namespace System.Cloud.DocumentDb;

public interface IDocumentDatabase
{
    IDocumentReader<TDocument> GetDocumentReader<TDocument>(TableOptions options) where TDocument : notnull;
    IDocumentWriter<TDocument> GetDocumentWriter<TDocument>(TableOptions options) where TDocument : notnull;
    Task ConnectAsync(bool createIfNotExists, CancellationToken cancellationToken);
    Task<IDatabaseResponse<bool>> DeleteDatabaseAsync(CancellationToken cancellationToken);
    Task<IDatabaseResponse<TableOptions>> ReadTableSettingsAsync(TableOptions tableOptions, RequestOptions requestOptions, CancellationToken cancellationToken);
    Task<IDatabaseResponse<bool>> UpdateTableSettingsAsync(TableOptions tableOptions, RequestOptions requestOptions, CancellationToken cancellationToken);
    Task<IDatabaseResponse<TableOptions>> CreateTableAsync(TableOptions tableOptions, RequestOptions requestOptions, CancellationToken cancellationToken);
    Task<IDatabaseResponse<TableOptions>> DeleteTableAsync(TableOptions tableOptions, RequestOptions requestOptions, CancellationToken cancellationToken);
}

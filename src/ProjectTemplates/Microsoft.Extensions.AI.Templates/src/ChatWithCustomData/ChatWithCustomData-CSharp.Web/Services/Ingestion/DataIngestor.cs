using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace ChatWithCustomData_CSharp.Web.Services.Ingestion;

public class DataIngestor(
    ILogger<DataIngestor> logger,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IVectorStore vectorStore)
{
    public static async Task IngestDataAsync(IServiceProvider services, IIngestionSource source)
    {
        using var scope = services.CreateScope();
        var ingestor = scope.ServiceProvider.GetRequiredService<DataIngestor>();
        await ingestor.IngestDataAsync(source);
    }

    public async Task IngestDataAsync(IIngestionSource source)
    {
#if (UseQdrant)
        var vectorCollection = vectorStore.GetCollection<Guid, SemanticSearchRecord>("data-ChatWithCustomData-CSharp.Web-ingestion");
#else
        var vectorCollection = vectorStore.GetCollection<string, SemanticSearchRecord>("data-ChatWithCustomData-CSharp.Web-ingestion");
#endif
        await vectorCollection.CreateCollectionIfNotExistsAsync();

        var recordsForSource = await vectorCollection.GetAsync(record => record.SourceId == source.SourceId, top: int.MaxValue).ToListAsync();
        var documentsForSource = recordsForSource.Select(r => new IngestedDocument(r.DocumentId, r.DocumentVersion)).Distinct().ToList();

        var deletedDocuments = await source.GetDeletedDocumentsAsync(documentsForSource);
        foreach (var deletedDocument in deletedDocuments)
        {
            logger.LogInformation("Removing ingested data for {documentId}", deletedDocument.DocumentId);
            await vectorCollection.DeleteAsync(recordsForSource.Where(r => r.DocumentId == deletedDocument.DocumentId).Select(r => r.Key));
        }

        var modifiedDocuments = await source.GetNewOrModifiedDocumentsAsync(documentsForSource);
        foreach (var modifiedDocument in modifiedDocuments)
        {
            logger.LogInformation("Processing {documentId}", modifiedDocument.DocumentId);

            var oldRecordsToDelete = recordsForSource.Where(r => r.DocumentId == modifiedDocument.DocumentId);
            if (oldRecordsToDelete.Any())
            {
                await vectorCollection.DeleteAsync(oldRecordsToDelete.Select(r => r.Key));
            }

            var newRecords = await source.CreateRecordsForDocumentAsync(embeddingGenerator, modifiedDocument);
            await vectorCollection.UpsertAsync(newRecords);
        }

        logger.LogInformation("Ingestion is up-to-date");
    }
}

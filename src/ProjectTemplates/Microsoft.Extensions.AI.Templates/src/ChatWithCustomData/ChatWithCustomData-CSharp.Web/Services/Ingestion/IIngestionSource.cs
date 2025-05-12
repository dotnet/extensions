using Microsoft.Extensions.AI;

namespace ChatWithCustomData_CSharp.Web.Services.Ingestion;

public interface IIngestionSource
{
    string SourceId { get; }

    Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocuments);

    Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IReadOnlyList<IngestedDocument> existingDocumentsIds);

    Task<IEnumerable<SemanticSearchRecord>> CreateRecordsForDocumentAsync(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, IngestedDocument document);
}

public record IngestedDocument(string DocumentId, string DocumentVersion);

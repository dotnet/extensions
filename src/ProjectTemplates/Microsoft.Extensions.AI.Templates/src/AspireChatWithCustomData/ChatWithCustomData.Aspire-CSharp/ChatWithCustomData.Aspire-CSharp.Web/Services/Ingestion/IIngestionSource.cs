﻿using Microsoft.Extensions.AI;

namespace ChatWithCustomData.Aspire_CSharp.Web.Services.Ingestion;

public interface IIngestionSource
{
    string SourceId { get; }

    Task<IEnumerable<IngestedDocument>> GetNewOrModifiedDocumentsAsync(IQueryable<IngestedDocument> existingDocuments);

    Task<IEnumerable<IngestedDocument>> GetDeletedDocumentsAsync(IQueryable<IngestedDocument> existingDocuments);

    Task<IEnumerable<SemanticSearchRecord>> CreateRecordsForDocumentAsync(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, string documentId);
}

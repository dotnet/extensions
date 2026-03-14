using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;

namespace AIChatWeb_CSharp.Web.Services.Ingestion;

public class DataIngestor(
    ILogger<DataIngestor> logger,
    ILoggerFactory loggerFactory,
#if (IsQdrant)
    VectorStoreCollection<Guid, IngestedChunk> vectorCollection,
#else
    VectorStoreCollection<string, IngestedChunk> vectorCollection,
#endif
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
    public async Task IngestDataAsync(DirectoryInfo directory, string searchPattern)
    {
#if (IsQdrant)
        using var writer = new VectorStoreWriter<Guid, string, IngestedChunk>(vectorCollection, new()
#else
        using var writer = new VectorStoreWriter<string, string, IngestedChunk>(vectorCollection, new()
#endif
        {
            IncrementalIngestion = false,
        });

        using var pipeline = new IngestionPipeline<string>(
            reader: new DocumentReader(directory),
            chunker: new SemanticSimilarityChunker(embeddingGenerator, new(TiktokenTokenizer.CreateForModel("gpt-4o"))),
            writer: writer,
            loggerFactory: loggerFactory);

        await foreach (var result in pipeline.ProcessAsync(directory, searchPattern))
        {
            logger.LogInformation("Completed processing '{id}'. Succeeded: '{succeeded}'.", result.DocumentId, result.Succeeded);
        }
    }
}

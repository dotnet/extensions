using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;

namespace aichatweb.Web.Services.Ingestion;

public class DataIngestor(
    ILogger<DataIngestor> logger,
    ILoggerFactory loggerFactory,
    VectorStoreCollection<string, IngestedChunk> vectorCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
    public async Task IngestDataAsync(DirectoryInfo directory, string searchPattern)
    {
        using var writer = new VectorStoreWriter<string, string, IngestedChunk>(vectorCollection, new()
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

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace ChatWithCustomData_CSharp.Web.Services;

public class SemanticSearch(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IVectorStore vectorStore)
{
    public async Task<IReadOnlyList<SemanticSearchRecord>> SearchAsync(string text, string? filenameFilter, int maxResults)
    {
        var queryEmbedding = await embeddingGenerator.GenerateEmbeddingVectorAsync(text);
        var vectorCollection = vectorStore.GetCollection<Guid, SemanticSearchRecord>("data-ChatWithCustomData-CSharp.Web-ingestion");
        // TODO: Use non-deprecated API
        var filter = filenameFilter is { Length: > 0 }
            ? new VectorSearchFilter().EqualTo(nameof(SemanticSearchRecord.FileName), filenameFilter)
            : null;

        // TODO: Use non-deprecated API
        var nearest = await vectorCollection.VectorizedSearchAsync(queryEmbedding, new VectorSearchOptions<SemanticSearchRecord>
        {
            Top = maxResults,
            OldFilter = filter,
        });
        var results = new List<SemanticSearchRecord>();
        await foreach (var item in nearest.Results)
        {
            results.Add(item.Record);
        }

        return results;
    }
}

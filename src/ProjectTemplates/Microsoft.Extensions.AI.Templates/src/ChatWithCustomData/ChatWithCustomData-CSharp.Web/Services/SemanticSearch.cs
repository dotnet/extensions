using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace ChatWithCustomData_CSharp.Web.Services;

public class SemanticSearch(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IVectorStore vectorStore)
{
    public async Task<IReadOnlyList<SemanticSearchRecord>> SearchAsync(string text, string? filenameFilter, int maxResults)
    {
        var queryEmbedding = await embeddingGenerator.GenerateVectorAsync(text);
#if (UseQdrant)
        var vectorCollection = vectorStore.GetCollection<Guid, SemanticSearchRecord>("data-ChatWithCustomData-CSharp.Web-ingestion");
#else
        var vectorCollection = vectorStore.GetCollection<string, SemanticSearchRecord>("data-ChatWithCustomData-CSharp.Web-ingestion");
#endif

        var nearest = await vectorCollection.VectorizedSearchAsync(queryEmbedding, new VectorSearchOptions<SemanticSearchRecord>
        {
            Top = maxResults,
            Filter = filenameFilter is { Length: > 0 } ? record => record.FileName == filenameFilter : null,
        });
        var results = new List<SemanticSearchRecord>();
        await foreach (var item in nearest.Results)
        {
            results.Add(item.Record);
        }

        return results;
    }
}

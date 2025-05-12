using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace ChatWithCustomData_CSharp.Web.Services;

public class SemanticSearch(
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    IVectorStore vectorStore)
{
    public async Task<IReadOnlyList<IngestedChunk>> SearchAsync(string text, string? documentIdFilter, int maxResults)
    {
        var queryEmbedding = await embeddingGenerator.GenerateVectorAsync(text);
#if (UseQdrant)
        var vectorCollection = vectorStore.GetCollection<Guid, IngestedChunk>("data-ChatWithCustomData-CSharp.Web-chunks");
#else
        var vectorCollection = vectorStore.GetCollection<string, IngestedChunk>("data-ChatWithCustomData-CSharp.Web-chunks");
#endif

        var nearest = vectorCollection.SearchEmbeddingAsync(queryEmbedding, maxResults, new VectorSearchOptions<IngestedChunk>
        {
            Filter = documentIdFilter is { Length: > 0 } ? record => record.DocumentId == documentIdFilter : null,
        });
        var results = new List<IngestedChunk>();
        await foreach (var item in nearest)
        {
            results.Add(item.Record);
        }

        return results;
    }
}

using Microsoft.Extensions.VectorData;

namespace ChatWithCustomData_CSharp.Web.Services;

public class SemanticSearch(
#if (UseQdrant)
    VectorStoreCollection<Guid, IngestedChunk> vectorCollection)
#else
    VectorStoreCollection<string, IngestedChunk> vectorCollection)
#endif
{
    public async Task<IReadOnlyList<IngestedChunk>> SearchAsync(string text, string? documentIdFilter, int maxResults)
    {
        var nearest = vectorCollection.SearchAsync(text, maxResults, new VectorSearchOptions<IngestedChunk>
        {
            Filter = documentIdFilter is { Length: > 0 } ? record => record.DocumentId == documentIdFilter : null,
        });

        return await nearest.Select(result => result.Record).ToListAsync();
    }
}

using Microsoft.Extensions.VectorData;

namespace AspireAIChat.Web.Services;

public class SemanticSearchRecord
{
    [VectorStoreRecordKey]
    public required string Key { get; set; }

    [VectorStoreRecordData]
    public required string FileName { get; set; }

    [VectorStoreRecordData]
    public int PageNumber { get; set; }

    [VectorStoreRecordData]
    public required string Text { get; set; }

    [VectorStoreRecordVector(384, DistanceFunction.CosineSimilarity)] // 384 is the default vector size for the all-minilm embedding model
    public ReadOnlyMemory<float> Vector { get; set; }
}

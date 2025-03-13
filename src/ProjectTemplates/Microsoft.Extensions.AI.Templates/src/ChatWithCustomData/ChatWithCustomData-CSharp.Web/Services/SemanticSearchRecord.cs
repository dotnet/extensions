using Microsoft.Extensions.VectorData;

namespace ChatWithCustomData_CSharp.Web.Services;

public class SemanticSearchRecord
{
    [VectorStoreRecordKey]
    public required Guid Key { get; set; }

    [VectorStoreRecordData]
    public required string FileName { get; set; }

    [VectorStoreRecordData]
    public int PageNumber { get; set; }

    [VectorStoreRecordData]
    public required string Text { get; set; }

#if (IsOllama)
    [VectorStoreRecordVector(384, DistanceFunction.CosineSimilarity)] // 384 is the default vector size for the all-minilm embedding model
#else
    [VectorStoreRecordVector(1536, DistanceFunction.CosineSimilarity)] // 1536 is the default vector size for the OpenAI text-embedding-3-small model
#endif
    public ReadOnlyMemory<float> Vector { get; set; }
}

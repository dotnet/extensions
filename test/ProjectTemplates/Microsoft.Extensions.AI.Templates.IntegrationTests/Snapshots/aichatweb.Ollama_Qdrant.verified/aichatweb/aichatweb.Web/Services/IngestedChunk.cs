using Microsoft.Extensions.VectorData;

namespace aichatweb.Web.Services;

public class IngestedChunk
{
    [VectorStoreRecordKey]
    public required Guid Key { get; set; }

    [VectorStoreRecordData(IsIndexed = true)]
    public required string DocumentId { get; set; }

    [VectorStoreRecordData]
    public int PageNumber { get; set; }

    [VectorStoreRecordData]
    public required string Text { get; set; }

    [VectorStoreRecordVector(384, DistanceFunction = DistanceFunction.CosineSimilarity)] // 384 is the default vector size for the all-minilm embedding model
    public ReadOnlyMemory<float> Vector { get; set; }
}

using Microsoft.Extensions.VectorData;

namespace aichatweb.Web.Services;

public class IngestedChunk
{
    private const int VectorDimensions = 384; // 384 is the default vector size for the all-minilm embedding model

    [VectorStoreKey]
    public required Guid Key { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public required string DocumentId { get; set; }

    [VectorStoreData]
    public int PageNumber { get; set; }

    [VectorStoreData]
    public required string Text { get; set; }

    [VectorStoreVector(VectorDimensions, DistanceFunction = DistanceFunction.EuclideanDistance)]
    public ReadOnlyMemory<float> Vector { get; set; }
}

using Microsoft.Extensions.VectorData;

namespace aichatweb.Web.Services;

public class IngestedChunk
{
    private const int VectorDimensions = 1536; // 1536 is the default vector size for the OpenAI text-embedding-3-small model
    private const string VectorDistanceFunction = DistanceFunction.CosineDistance;

    [VectorStoreKey]
    public required string Key { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public required string DocumentId { get; set; }

    [VectorStoreData]
    public int PageNumber { get; set; }

    [VectorStoreData]
    public required string Text { get; set; }

    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction)]
    public string? Vector => Text;
}

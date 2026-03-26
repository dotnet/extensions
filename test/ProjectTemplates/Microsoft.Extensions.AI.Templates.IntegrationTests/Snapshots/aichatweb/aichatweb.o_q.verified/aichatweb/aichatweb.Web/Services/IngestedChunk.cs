using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.VectorData;

namespace aichatweb.Web.Services;

public class IngestedChunk : IngestionChunkVectorRecord<string>
{
    public const int VectorDimensions = 384; // 384 is the default vector size for the all-minilm embedding model
    public const string VectorDistanceFunction = DistanceFunction.CosineSimilarity;
    public const string CollectionName = "data-aichatweb-chunks";

    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction)]
    public override string? Embedding => Content;
}

using System.Text.Json.Serialization;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.VectorData;

namespace aichatweb.Web.Services;

public class IngestedChunk : IngestedChunkRecord<string>
{
    public const int VectorDimensions = 1536; // 1536 is the default vector size for the OpenAI text-embedding-3-small model
    public const string VectorDistanceFunction = DistanceFunction.CosineSimilarity;
    public const string CollectionName = "data-aichatweb-chunks";

    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction, StorageName = "embedding")]
    public override string? Embedding => Content;
}

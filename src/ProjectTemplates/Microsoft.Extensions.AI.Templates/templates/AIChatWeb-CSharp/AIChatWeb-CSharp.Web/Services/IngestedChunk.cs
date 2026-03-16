using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.VectorData;

namespace AIChatWeb_CSharp.Web.Services;

public class IngestedChunk : IngestedChunkRecord<string>
{
#if (IsOllama)
    public const int VectorDimensions = 384; // 384 is the default vector size for the all-minilm embedding model
#else
    public const int VectorDimensions = 1536; // 1536 is the default vector size for the OpenAI text-embedding-3-small model
#endif
#if (IsAzureAISearch || IsQdrant)
    public const string VectorDistanceFunction = DistanceFunction.CosineSimilarity;
#else
    public const string VectorDistanceFunction = DistanceFunction.CosineDistance;
#endif
    public const string CollectionName = "data-AIChatWeb-CSharp.Web-chunks";

    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction, StorageName = EmbeddingStorageName)]
    public override string? Embedding => Content;
}

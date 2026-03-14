using System.Text.Json.Serialization;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.VectorData;

namespace AIChatWeb_CSharp.Web.Services;

#if (IsQdrant)
public class IngestedChunk : IngestedChunkRecord<Guid, string>
#else
public class IngestedChunk : IngestedChunkRecord<string, string>
#endif
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

    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction, StorageName = EmbeddingPropertyName)]
    [JsonPropertyName(EmbeddingPropertyName)]
    public override string? Embedding => Content;
}

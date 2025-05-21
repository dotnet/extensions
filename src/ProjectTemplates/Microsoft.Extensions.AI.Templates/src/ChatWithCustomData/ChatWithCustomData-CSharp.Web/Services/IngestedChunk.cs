using Microsoft.Extensions.VectorData;

namespace ChatWithCustomData_CSharp.Web.Services;

public class IngestedChunk
{
#if (IsOllama)
    private const int VectorDimensions = 384; // 384 is the default vector size for the all-minilm embedding model
#else
    private const int VectorDimensions = 1536; // 1536 is the default vector size for the OpenAI text-embedding-3-small model
#endif
#if (UseAzureAISearch || UseQdrant)
    private const string VectorDistanceFunction = DistanceFunction.CosineSimilarity;
#else
    private const string VectorDistanceFunction = DistanceFunction.CosineDistance;
#endif

    [VectorStoreKey]
#if (UseQdrant)
    public required Guid Key { get; set; }
#else
    public required string Key { get; set; }
#endif

    [VectorStoreData(IsIndexed = true)]
    public required string DocumentId { get; set; }

    [VectorStoreData]
    public int PageNumber { get; set; }

    [VectorStoreData]
    public required string Text { get; set; }

    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction)]
    public string? Vector => Text;
}

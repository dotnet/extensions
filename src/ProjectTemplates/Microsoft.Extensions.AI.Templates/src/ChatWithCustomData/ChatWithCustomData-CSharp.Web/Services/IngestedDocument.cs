using Microsoft.Extensions.VectorData;

namespace ChatWithCustomData_CSharp.Web.Services;

public class IngestedDocument
{
#if (UseAzureAISearch || UseQdrant)
    const string VectorDistanceFunction = DistanceFunction.CosineSimilarity;
#else
    const string VectorDistanceFunction = DistanceFunction.CosineDistance;
#endif

    [VectorStoreKey]
#if (UseQdrant)
    public required Guid Key { get; set; }
#else
    public required string Key { get; set; }
#endif

    [VectorStoreData(IsIndexed = true)]
    public required string SourceId { get; set; }

    [VectorStoreData]
    public required string DocumentId { get; set; }

    [VectorStoreData]
    public required string DocumentVersion { get; set; }

    // The vector is not used but required for some vector databases
    [VectorStoreVector(1, DistanceFunction = VectorDistanceFunction)]
    public ReadOnlyMemory<float> Vector { get; set; } = new ReadOnlyMemory<float>([0]);
}

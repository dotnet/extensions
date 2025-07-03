using Microsoft.Extensions.VectorData;

namespace ChatWithCustomData_CSharp.Web.Services;

public class IngestedDocument
{
    private const int VectorDimensions = 2;
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
    public required string SourceId { get; set; }

    [VectorStoreData]
    public required string DocumentId { get; set; }

    [VectorStoreData]
    public required string DocumentVersion { get; set; }

    // The vector is not used but required for some vector databases
    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction)]
    public ReadOnlyMemory<float> Vector { get; set; } = new ReadOnlyMemory<float>([0, 0]);
}

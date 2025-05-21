using Microsoft.Extensions.VectorData;

namespace aichatweb.Web.Services;

public class IngestedDocument
{
    private const int VectorDimensions = 2;
    private const string VectorDistanceFunction = DistanceFunction.CosineSimilarity;

    [VectorStoreKey]
    public required string Key { get; set; }

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

using Microsoft.Extensions.VectorData;

namespace aichatweb.Web.Services;

public class IngestedDocument
{
    [VectorStoreRecordKey]
    public required string Key { get; set; }

    [VectorStoreRecordData(IsIndexed = true)]
    public required string SourceId { get; set; }

    [VectorStoreRecordData]
    public required string DocumentId { get; set; }

    [VectorStoreRecordData]
    public required string DocumentVersion { get; set; }

    // The vector is not used but required for some vector databases
    [VectorStoreRecordVector(2, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; } = new ReadOnlyMemory<float>([0, 0]);
}

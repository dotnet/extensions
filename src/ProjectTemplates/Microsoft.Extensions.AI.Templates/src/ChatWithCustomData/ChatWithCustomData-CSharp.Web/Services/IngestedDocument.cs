using Microsoft.Extensions.VectorData;

namespace ChatWithCustomData_CSharp.Web.Services;

public class IngestedDocument
{
    [VectorStoreRecordKey]
#if (UseQdrant)
    public required Guid Key { get; set; }
#else
    public required string Key { get; set; }
#endif

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

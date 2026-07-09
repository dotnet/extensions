using System.Text.Json.Serialization;
using Microsoft.Extensions.VectorData;

namespace aichatweb.Services;

public class IngestedChunk
{
    public const int VectorDimensions = 1024; // 1024 is the default vector size for the qwen3-embedding-0.6b embedding model
    public const string VectorDistanceFunction = DistanceFunction.CosineDistance;
    public const string CollectionName = "data-aichatweb-chunks";

    [VectorStoreKey(StorageName = "key")]
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    [VectorStoreData(StorageName = "documentid")]
    [JsonPropertyName("documentid")]
    public required string DocumentId { get; set; }

    [VectorStoreData(StorageName = "content")]
    [JsonPropertyName("content")]
    public required string Text { get; set; }

    [VectorStoreData(StorageName = "context")]
    [JsonPropertyName("context")]
    public string? Context { get; set; }

    [VectorStoreVector(VectorDimensions, DistanceFunction = VectorDistanceFunction, StorageName = "embedding")]
    [JsonPropertyName("embedding")]
    public string? Vector => Text;
}

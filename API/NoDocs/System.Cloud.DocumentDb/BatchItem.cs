// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public readonly struct BatchItem<T>
{
    public BatchOperation Operation { get; }
    public T? Item { get; }
    public string? Id { get; }
    public string? ItemVersion { get; }
    public BatchItem(BatchOperation operation, T? item = default(T?), string? id = null, string? itemVersion = null);
}

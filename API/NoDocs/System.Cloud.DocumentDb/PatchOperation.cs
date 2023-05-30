// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public readonly struct PatchOperation
{
    public PatchOperationType OperationType { get; }
    public string Path { get; }
    public object Value { get; }
    public static PatchOperation Add<T>(string path, T value) where T : notnull;
    public static PatchOperation Remove(string path);
    public static PatchOperation Replace<T>(string path, T value) where T : notnull;
    public static PatchOperation Set<T>(string path, T value) where T : notnull;
    public static PatchOperation Increment(string path, long value);
    public static PatchOperation Increment(string path, double value);
}

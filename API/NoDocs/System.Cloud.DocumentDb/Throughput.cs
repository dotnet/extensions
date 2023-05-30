// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

public readonly struct Throughput
{
    public static readonly Throughput Unlimited;
    public int? Value { get; }
    public Throughput(int? throughput);
}

// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// The structure to define throughput.
/// </summary>
public readonly struct Throughput
{
    /// <summary>
    /// The constant for unlimited throughput.
    /// </summary>
    public static readonly Throughput Unlimited;

    /// <summary>
    /// Gets throughput value.
    /// </summary>
    /// <remarks>
    /// The throughput is in database defined units,
    /// for example, Cosmos DB throughput measured in RUs (request units) per second:
    /// <see href="https://learn.microsoft.com/azure/cosmos-db/concepts-limits">Azure Cosmos DB service quotas</see>.
    /// </remarks>
    public int? Value { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.Throughput" /> struct.
    /// </summary>
    /// <param name="throughput">The throughput.</param>
    /// <remarks>
    /// See <see cref="P:System.Cloud.DocumentDb.Throughput.Value" /> for more details.
    /// </remarks>
    public Throughput(int? throughput);
}

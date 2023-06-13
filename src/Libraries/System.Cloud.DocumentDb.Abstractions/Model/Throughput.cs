// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Cloud.DocumentDb;

/// <summary>
/// The structure to define throughput.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types",
    Justification = "The struct should not be used as a hash key.")]
public readonly struct Throughput
{
    /// <summary>
    /// The constant for unlimited throughput.
    /// </summary>
    public static readonly Throughput Unlimited = new(null);

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
    /// Initializes a new instance of the <see cref="Throughput"/> struct.
    /// </summary>
    /// <param name="throughput">The throughput.</param>
    /// <remarks>
    /// See <see cref="Value"/> for more details.
    /// </remarks>
    public Throughput(int? throughput)
    {
        Value = throughput;
    }
}

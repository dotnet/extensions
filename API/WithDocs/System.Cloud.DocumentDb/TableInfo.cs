// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Represents read-only table configurations.
/// </summary>
/// <remarks>
/// Contains similar information as <see cref="T:System.Cloud.DocumentDb.TableOptions" />,
/// but can't be extended and modified.
/// It's designed to be used in a hot pass,
/// and has 8x performance compared to using <see cref="T:System.Cloud.DocumentDb.TableOptions" />.
/// </remarks>
public readonly struct TableInfo
{
    /// <summary>
    /// Gets the table name.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="F:System.String.Empty" />.
    /// The value is required.
    /// </remarks>
    public string TableName { get; }

    /// <summary>
    /// Gets the time to live for table items.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// If not specified, records will not expire.
    /// 1s is the minimum value.
    /// </remarks>
    public TimeSpan TimeToLive { get; }

    /// <summary>
    /// Gets the partition id path for store.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// </remarks>
    public string? PartitionIdPath { get; }

    /// <summary>
    /// Gets a value indicating whether table is regionally replicated or a global.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="false" />, which means table is global.
    /// When enabling regional tables
    /// - All required region endpoints should be configured in client.
    /// - Requests should contain <see cref="P:System.Cloud.DocumentDb.RequestOptions.Region" /> provided.
    /// </remarks>
    public bool IsRegional { get; }

    /// <summary>
    /// Gets the table throughput value.
    /// </summary>
    /// <value>
    /// The default is <see cref="F:System.Cloud.DocumentDb.Throughput.Unlimited" />.
    /// </value>
    /// <seealso cref="P:System.Cloud.DocumentDb.Throughput.Value" />
    public Throughput Throughput { get; }

    /// <summary>
    /// Gets a value indicating whether a <see cref="T:System.Cloud.DocumentDb.ITableLocator" /> is required to be used with this table.
    /// </summary>
    /// <value>
    /// The default is <see langword="false" />, which means a locator isn't used even if configured.
    /// </value>
    /// <remarks>
    /// If a locator is required, requests require that <see cref="T:System.Cloud.DocumentDb.RequestOptions" /> be specified to provide <see cref="P:System.Cloud.DocumentDb.RequestOptions`1.Document" />.
    /// This is a protection mechanism to ensure that the table forgets provided documents.
    /// </remarks>
    public bool IsLocatorRequired { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.TableInfo" /> struct.
    /// </summary>
    /// <param name="options">The table options.</param>
    public TableInfo(TableOptions options);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:System.Cloud.DocumentDb.TableInfo" /> struct.
    /// </summary>
    /// <param name="info">The source table info.</param>
    /// <param name="tableNameOverride">The table name.</param>
    /// <param name="isRegionalOverride"><see langword="true" /> to mark the table as regional; otherwise, <see langword="false" />.</param>
    public TableInfo(in TableInfo info, string? tableNameOverride = null, bool? isRegionalOverride = null);
}

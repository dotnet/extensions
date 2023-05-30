// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Shared.Data.Validation;

namespace System.Cloud.DocumentDb;

/// <summary>
/// The class representing table configurations.
/// </summary>
public class TableOptions
{
    /// <summary>
    /// Gets or sets the table name.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="F:System.String.Empty" />.
    /// The value is required.
    /// </remarks>
    [Required]
    public string TableName { get; set; }

    /// <summary>
    /// Gets or sets the time to live for table items.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// If not specified, records will not expire.
    /// 1s is the minimum value.
    /// </remarks>
    [TimeSpan(1000)]
    public TimeSpan TimeToLive { get; set; }

    /// <summary>
    /// Gets or sets the partition id path for store.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// </remarks>
    public string? PartitionIdPath { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether table is regionally replicated or a global.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="false" />, which means table is global.
    /// When enabling regional tables
    /// - All required region endpoints should be configured in client.
    /// - Requests should contain <see cref="P:System.Cloud.DocumentDb.RequestOptions.Region" /> provided.
    /// </remarks>
    public bool IsRegional { get; set; }

    /// <summary>
    /// Gets or sets the table throughput value.
    /// </summary>
    /// <value>
    /// The default is <see cref="F:System.Cloud.DocumentDb.Throughput.Unlimited" />.
    /// </value>
    /// <seealso cref="P:System.Cloud.DocumentDb.Throughput.Value" />
    public Throughput Throughput { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a <see cref="T:System.Cloud.DocumentDb.ITableLocator" /> required to be used with this table.
    /// </summary>
    /// <value>
    /// The default is <see langword="false" />, which means a locator will not be used even if configured.
    /// </value>
    /// <remarks>
    /// If locator is required, requests will require <see cref="T:System.Cloud.DocumentDb.RequestOptions" /> provided to API to provide <see cref="P:System.Cloud.DocumentDb.RequestOptions`1.Document" />.
    /// This is the protection mechanism to avoid engineers not designed specific table to forget provide documents when table locator is in use.
    /// </remarks>
    public bool IsLocatorRequired { get; set; }

    public TableOptions();
}

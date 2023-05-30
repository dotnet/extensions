// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Shared.Data.Validation;

namespace System.Cloud.DocumentDb;

/// <summary>
/// The class representing configurations for database.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Gets or sets the global database name.
    /// </summary>
    /// <remarks>
    /// Default is <see cref="F:System.String.Empty" />.
    /// The value is required.
    /// </remarks>
    [Required]
    public string DatabaseName { get; set; }

    /// <summary>
    /// Gets or sets default name for a database in regions.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// This name can be override by specific region config <see cref="P:System.Cloud.DocumentDb.RegionalDatabaseOptions.DatabaseName" />.
    /// The value is required if regional name has not overridden.
    /// </remarks>
    public string? DefaultRegionalDatabaseName { get; set; }

    /// <summary>
    /// Gets or sets the key to the account or resource token.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// </remarks>
    public string? PrimaryKey { get; set; }

    /// <summary>
    /// Gets or sets the global database endpoint uri.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// The value is required.
    /// </remarks>
    [Required]
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets timeout before unused connection will be closed.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// By default, idle connections should be kept open indefinitely.
    /// Value must be greater than or equal to 10 minutes.
    /// Recommended values are between 20 minutes and 24 hours.
    /// Mainly useful for sparse infrequent access to a large database account.
    /// Works for all global and regional connections.
    /// </remarks>
    [TimeSpan("00:10:00", "30.00:00:00")]
    public TimeSpan? IdleTcpConnectionTimeout { get; set; }

    /// <summary>
    /// Gets or sets the throughput value.
    /// </summary>
    /// <remarks>
    /// The default is <see cref="F:System.Cloud.DocumentDb.Throughput.Unlimited" />.
    /// The throughput is in database defined units,
    /// for example, Cosmos DB throughput measured in RUs (request units) per second:
    /// <see href="https://learn.microsoft.com/azure/cosmos-db/concepts-limits">Azure Cosmos DB service quotas</see>.
    /// </remarks>
    public Throughput Throughput { get; set; }

    /// <summary>
    /// Gets or sets json serializer options.
    /// </summary>
    /// <remarks>
    /// This will be used only if <see cref="P:System.Cloud.DocumentDb.DatabaseOptions.OverrideSerialization" /> is enabled.
    /// Default is the default <see cref="P:System.Cloud.DocumentDb.DatabaseOptions.JsonSerializerOptions" />.
    /// Those options will be used by compatible APIs to serialize input before sending to server and deserialize output.
    /// This includes sent/received documents.
    /// </remarks>
    public JsonSerializerOptions JsonSerializerOptions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether serialization overridden.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="true" />.
    /// When enabled, System.Text.Json based serialization will be configured with
    /// settings defined in <see cref="P:System.Cloud.DocumentDb.DatabaseOptions.JsonSerializerOptions" />.
    /// </remarks>
    [Experimental("EXTEXP0011", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public bool OverrideSerialization { get; set; }

    /// <summary>
    /// Gets or sets a list of preferred regions used for SDK to define failover order for global database.
    /// </summary>
    /// <value>
    /// The default value is empty <see cref="T:System.Collections.Generic.List`1" />.
    /// </value>
    public IList<string> FailoverRegions { get; set; }

    /// <summary>
    /// Gets or sets a list of region specific configurations for the database.
    /// </summary>
    /// <value>
    /// The default value is empty <see cref="T:System.Collections.Generic.Dictionary`2" />.
    /// </value>
    public IDictionary<string, RegionalDatabaseOptions> RegionalDatabaseOptions { get; set; }

    public DatabaseOptions();
}

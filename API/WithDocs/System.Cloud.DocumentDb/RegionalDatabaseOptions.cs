// Assembly 'System.Cloud.DocumentDb.Abstractions'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace System.Cloud.DocumentDb;

/// <summary>
/// The class representing region specific configurations for database.
/// </summary>
public class RegionalDatabaseOptions
{
    /// <summary>
    /// Gets or sets the regional database name.
    /// </summary>
    /// <value>The default value is <see langword="null" />.</value>
    /// <remarks>
    /// If the value is not specified, <see cref="P:System.Cloud.DocumentDb.DatabaseOptions.DefaultRegionalDatabaseName" /> is used.
    /// </remarks>
    public string? DatabaseName { get; set; }

    /// <summary>
    /// Gets or sets the regional database endpoint.
    /// </summary>
    /// <remarks>
    /// The value is required.
    /// </remarks>
    [Required]
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the key to the account or resource token.
    /// </summary>
    /// <value>The default value is <see langword="null" />.</value>
    public string? PrimaryKey { get; set; }

    /// <summary>
    /// Gets or sets a list of preferred regions used for SDK to define failover order for regional database.
    /// </summary>
    /// <value>
    /// The default value is an empty <see cref="T:System.Collections.Generic.Dictionary`2" />.
    /// </value>
    public IList<string> FailoverRegions { get; set; }

    public RegionalDatabaseOptions();
}

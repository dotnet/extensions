// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
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
    /// Default is <see cref="string.Empty" />.
    /// The value is required.
    /// </remarks>
    [Required]
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets default name for a database in regions.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// This name can be override by specific region config <see cref="RegionalDatabaseOptions.DatabaseName"/>.
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
    /// The default is <see cref="Throughput.Unlimited"/>.
    /// The throughput is in database defined units,
    /// for example, Cosmos DB throughput measured in RUs (request units) per second:
    /// <see href="https://learn.microsoft.com/azure/cosmos-db/concepts-limits">Azure Cosmos DB service quotas</see>.
    /// </remarks>
    public Throughput Throughput { get; set; } = Throughput.Unlimited;

    /// <summary>
    /// Gets or sets json serializer options.
    /// </summary>
    /// <remarks>
    /// This will be used only if <see cref="OverrideSerialization"/> is enabled.
    /// Default is the default <see cref="JsonSerializerOptions" />.
    /// Those options will be used by compatible APIs to serialize input before sending to server and deserialize output.
    /// This includes sent/received documents.
    /// </remarks>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether serialization overridden.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="true"/>.
    /// When enabled, System.Text.Json based serialization will be configured with
    /// settings defined in <see cref="JsonSerializerOptions"/>.
    /// </remarks>
    [Experimental("New feature.")]
    public bool OverrideSerialization { get; set; } = true;

    /// <summary>
    /// Gets or sets a list of preferred regions used for SDK to define failover order for global database.
    /// </summary>
    /// <value>
    /// The default value is empty <see cref="List{T}" />.
    /// </value>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    public IList<string> FailoverRegions { get; set; }
        = new List<string>();

    /// <summary>
    /// Gets or sets a list of region specific configurations for the database.
    /// </summary>
    /// <value>
    /// The default value is empty <see cref="Dictionary{TKey, TValue}" />.
    /// </value>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only",
        Justification = "Options pattern.")]
    public IDictionary<string, RegionalDatabaseOptions> RegionalDatabaseOptions { get; set; }
        = new Dictionary<string, RegionalDatabaseOptions>();
}

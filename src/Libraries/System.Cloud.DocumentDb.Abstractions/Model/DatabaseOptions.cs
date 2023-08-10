// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.Shared.Data.Validation;
using Microsoft.Shared.DiagnosticIds;

namespace System.Cloud.DocumentDb;

/// <summary>
/// Represents configuration options for a database.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Gets or sets the global database name.
    /// </summary>
    /// <value>
    /// The default is <see cref="string.Empty" />.
    /// </value>
    /// <remarks>
    /// The value is required.
    /// </remarks>
    [Required]
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default name for a regional database.
    /// </summary>
    /// <value>
    /// The default is <see langword="null" />.
    /// </value>
    /// <remarks>
    /// This name can be overridden by a specific region config <see cref="RegionalDatabaseOptions.DatabaseName"/>.
    /// The value is required if the regional name isn't overridden.
    /// </remarks>
    public string? DefaultRegionalDatabaseName { get; set; }

    /// <summary>
    /// Gets or sets the key to the account or resource token.
    /// </summary>
    /// <value>
    /// The default is <see langword="null" />.
    /// </value>
    public string? PrimaryKey { get; set; }

    /// <summary>
    /// Gets or sets the global database endpoint uri.
    /// </summary>
    /// <value>
    /// The default is <see langword="null" />.
    /// </value>
    /// <remarks>
    /// The value is required.
    /// </remarks>
    [Required]
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the timeout before an unused connection is closed.
    /// </summary>
    /// <value>
    /// The default is <see langword="null" />.
    /// </value>
    /// <remarks>
    /// By default, idle connections should be kept open indefinitely.
    /// The value must be greater than or equal to 10 minutes.
    /// Recommended values are between 20 minutes and 24 hours.
    /// This value is mainly useful for sparse infrequent access to a large database account.
    /// It works for all global and regional connections.
    /// </remarks>
    [TimeSpan("00:10:00", "30.00:00:00")]
    public TimeSpan? IdleTcpConnectionTimeout { get; set; }

    /// <summary>
    /// Gets or sets the throughput value.
    /// </summary>
    /// <value>
    /// The default is <see cref="Throughput.Unlimited"/>.
    /// </value>
    /// <remarks>
    /// The throughput is in database defined units,
    /// for example, Cosmos DB throughput measured in RUs (request units) per second:
    /// <see href="https://learn.microsoft.com/azure/cosmos-db/concepts-limits">Azure Cosmos DB service quotas</see>.
    /// </remarks>
    public Throughput Throughput { get; set; } = Throughput.Unlimited;

    /// <summary>
    /// Gets or sets JSON serializer options.
    /// </summary>
    /// <value>
    /// The default is the default <see cref="JsonSerializerOptions" />.
    /// </value>
    /// <remarks>
    /// This will be used only if <see cref="OverrideSerialization"/> is enabled.
    /// Those options will be used by compatible APIs to serialize input before sending to server and deserialize output.
    /// This includes sent/received documents.
    /// </remarks>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether serialization overridden.
    /// </summary>
    /// <value>
    /// The default is <see langword="true"/>.
    /// </value>
    /// <remarks>
    /// When enabled, System.Text.Json based serialization will be configured with
    /// settings defined in <see cref="JsonSerializerOptions"/>.
    /// </remarks>
    [Experimental(diagnosticId: Experiments.DocumentDb, UrlFormat = Experiments.UrlFormat)]
    public bool OverrideSerialization { get; set; } = true;

    /// <summary>
    /// Gets or sets a list of preferred regions used for SDK to define failover order for global database.
    /// </summary>
    /// <value>
    /// The default value is empty <see cref="List{T}" />.
    /// </value>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Options pattern.")]
    public IList<string> FailoverRegions { get; set; }
        = new List<string>();

    /// <summary>
    /// Gets or sets a list of region specific configurations for the database.
    /// </summary>
    /// <value>
    /// The default value is empty <see cref="Dictionary{TKey, TValue}" />.
    /// </value>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Options pattern.")]
    public IDictionary<string, RegionalDatabaseOptions> RegionalDatabaseOptions { get; set; }
        = new Dictionary<string, RegionalDatabaseOptions>();
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
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
    /// Default is <see cref="string.Empty" />.
    /// The value is required.
    /// </remarks>
    [Required]
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time to live for table items.
    /// </summary>
    /// <remarks>
    /// Default is <see langword="null" />.
    /// If not specified, records will not expire.
    /// 1s is the minimum value.
    /// </remarks>
    [TimeSpan(1000)]
    public TimeSpan TimeToLive { get; set; } = Timeout.InfiniteTimeSpan;

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
    /// Default is <see langword="false"/>, which means table is global.
    /// When enabling regional tables
    /// - All required region endpoints should be configured in client.
    /// - Requests should contain <see cref="RequestOptions.Region"/> provided.
    /// </remarks>
    public bool IsRegional { get; set; }

    /// <summary>
    /// Gets or sets the table throughput value.
    /// </summary>
    /// <value>
    /// The default is <see cref="Throughput.Unlimited"/>.
    /// </value>
    /// <seealso cref="Throughput.Value"/>
    public Throughput Throughput { get; set; } = Throughput.Unlimited;

    /// <summary>
    /// Gets or sets a value indicating whether a <see cref="ITableLocator"/> required to be used with this table.
    /// </summary>
    /// <value>
    /// The default is <see langword="false"/>, which means a locator will not be used even if configured.
    /// </value>
    /// <remarks>
    /// If locator is required, requests will require <see cref="RequestOptions"/> provided to API to provide <see cref="RequestOptions{TDocument}.Document"/>.
    /// This is the protection mechanism to avoid engineers not designed specific table to forget provide documents when table locator is in use.
    /// </remarks>
    public bool IsLocatorRequired { get; set; }
}

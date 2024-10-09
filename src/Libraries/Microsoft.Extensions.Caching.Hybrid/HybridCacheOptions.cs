// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Options for configuring the default <see cref="HybridCache"/> implementation.
/// </summary>
public class HybridCacheOptions
{
    private const int ShiftBytesToMibiBytes = 20;

    /// <summary>
    /// Gets or sets the default global options to be applied to <see cref="HybridCache"/> operations.
    /// </summary>
    /// <remarks>
    /// If options are specified at the individual call level, the non-null values are merged
    /// (with the per-call options being used in preference to the global options). If no value is
    /// specified for a given option (globally or per-call), the implementation can choose a reasonable default.
    /// </remarks>
    public HybridCacheEntryOptions? DefaultEntryOptions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether compression for this <see cref="HybridCache"/> instance is disabled.
    /// </summary>
    public bool DisableCompression { get; set; }

    /// <summary>
    /// Gets or sets the maximum size of cache items.
    /// </summary>
    /// <value>
    /// The maximum size of cache items. The default value is 1 MiB.
    /// </value>
    /// <remarks>
    /// Attempts to store values over this size are logged,
    /// and the value isn't stored in the cache.
    /// </remarks>
    public long MaximumPayloadBytes { get; set; } = 1 << ShiftBytesToMibiBytes; // 1MiB

    /// <summary>
    /// Gets or sets the maximum permitted length (in characters) of keys.
    /// </summary>
    /// <value>
    /// The maximum permitted length of keys, in characters. The default value is 1024 characters.
    /// </value>
    /// <remarks>Attempts to use keys over this size are logged.</remarks>
    public int MaximumKeyLength { get; set; } = 1024; // characters

    /// <summary>
    /// Gets or sets a value indicating whether to use "tags" data as dimensions on metric reporting.
    /// </summary>
    /// <value>
    /// <see langword="true"> to use "tags" data as dimensions on metric reporting; otherwise, <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// If enabled, take care to ensure that tags don't contain data that
    /// should not be visible in metrics systems.
    /// </remarks>
    public bool ReportTagMetrics { get; set; }
}

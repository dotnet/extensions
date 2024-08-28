// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Options for configuring the default <see cref="HybridCache"/> implementation.
/// </summary>
public class HybridCacheOptions
{
    /// <summary>
    /// Gets or sets the default global options to be applied to <see cref="HybridCache"/> operations; if options are
    /// specified at the individual call level, the non-null values are merged (with the per-call
    /// options being used in preference to the global options). If no value is specified for a given
    /// option (globally or per-call), the implementation may choose a reasonable default.
    /// </summary>
    public HybridCacheEntryOptions? DefaultEntryOptions { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether compression for this <see cref="HybridCache"/> instance is disabled.
    /// </summary>
    public bool DisableCompression { get; set; }

    /// <summary>
    /// Gets or sets the maximum size of cache items; attempts to store values over this size will be logged
    /// and the value will not be stored in cache.
    /// </summary>
    /// <remarks>The default value is 1 MiB.</remarks>
    public long MaximumPayloadBytes { get; set; } = 1 << ShiftBytesToMibiBytes; // 1MiB

    /// <summary>
    /// Gets or sets the maximum permitted length (in characters) of keys; attempts to use keys over this size will be logged.
    /// </summary>
    /// <remark>The default value is 1024 characters.</remark>
    public int MaximumKeyLength { get; set; } = 1024; // characters

    /// <summary>
    /// Gets or sets a value indicating whether to use "tags" data as dimensions on metric reporting; if enabled, care should be used to ensure that
    /// tags do not contain data that should not be visible in metrics systems.
    /// </summary>
    public bool ReportTagMetrics { get; set; }

    private const int ShiftBytesToMibiBytes = 20;
}

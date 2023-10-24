﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.Enrichment;

/// <summary>
/// Constants used for enrichment tags.
/// </summary>
public static class ProcessEnricherTagNames
{
    /// <summary>
    /// Process ID.
    /// </summary>
    // See https://github.com/open-telemetry/semantic-conventions/blob/main/docs/resource/process.md#process.
    public const string ProcessId = "process.pid";

    /// <summary>
    /// Thread ID.
    /// </summary>
    // See https://github.com/open-telemetry/semantic-conventions/blob/main/docs/general/attributes.md#general-thread-attributes.
    public const string ThreadId = "thread.id";

    /// <summary>
    /// Gets a list of all dimension names.
    /// </summary>
    /// <returns>A read-only <see cref="IReadOnlyList{String}"/> of all dimension names.</returns>
    public static IReadOnlyList<string> DimensionNames { get; } =
        Array.AsReadOnly(new[]
        {
            ProcessId,
            ThreadId
        });
}

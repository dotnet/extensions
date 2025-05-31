// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring.Linux.Disk;

/// <summary>
/// An interface for reading disk statistics.
/// </summary>
internal interface IDiskStatsReader
{
    /// <summary>
    /// Gets all the disk statistics from the system.
    /// </summary>
    /// <returns>List of <see cref="DiskStats"/> instances.</returns>
    List<DiskStats> ReadAll();
}

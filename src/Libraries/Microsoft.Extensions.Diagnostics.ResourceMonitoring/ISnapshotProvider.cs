// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.ResourceMonitoring;

/// <summary>
/// An interface to be implemented by a provider that represents an underlying system and gets resources data about it.
/// </summary>
internal interface ISnapshotProvider
{
    /// <summary>
    /// Gets the static values of CPU and memory limitations defined by the system.
    /// </summary>
    SystemResources Resources { get; }

    /// <summary>
    /// Get a snapshot of the resource utilization of the system.
    /// </summary>
    /// <returns>An appropriate sample.</returns>
#pragma warning disable S4049 // Properties should be preferred
    Snapshot GetSnapshot();
#pragma warning restore S4049 // Properties should be preferred
}

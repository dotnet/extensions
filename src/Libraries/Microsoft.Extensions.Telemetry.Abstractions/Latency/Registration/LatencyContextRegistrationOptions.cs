// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Telemetry.Latency;

/// <summary>
/// Registered names for <see cref="ILatencyContext"/>.
/// </summary>
public class LatencyContextRegistrationOptions
{
    /// <summary>
    /// Gets or sets the list of registered checkpoint names.
    /// </summary>
    [Required]
    public IReadOnlyList<string> CheckpointNames { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of registered measure names.
    /// </summary>
    [Required]
    public IReadOnlyList<string> MeasureNames { get; set; } = new List<string>();

    /// <summary>
    /// Gets or sets the list of registered tag names.
    /// </summary>
    [Required]
    public IReadOnlyList<string> TagNames { get; set; } = new List<string>();

    internal void AddTagNames(string[] names) => AddToList(TagNames, names);
    internal void AddCheckpointNames(string[] names) => AddToList(CheckpointNames, names);
    internal void AddMeasureNames(string[] names) => AddToList(MeasureNames, names);

    private static void AddToList(IReadOnlyList<string> list, string[] names)
    {
        if (list is List<string> l)
        {
            l.AddRange(names);
        }
    }
}

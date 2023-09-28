// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Diagnostics.Latency.Internal;

/// <summary>
/// Class that holds registry of names used with <see cref="LatencyContext"/> APIs.
/// </summary>
internal sealed class LatencyContextRegistrySet
{
    /// <summary>
    /// Gets the registry of checkpoint names.
    /// </summary>
    public Registry CheckpointNameRegistry { get; }

    /// <summary>
    /// Gets the registry of tag names.
    /// </summary>
    public Registry TagNameRegistry { get; }

    /// <summary>
    /// Gets the registry of counter names.
    /// </summary>
    public Registry MeasureNameRegistry { get; }

    public LatencyContextRegistrySet(IOptions<LatencyContextOptions> latencyContextOptions,
        IOptions<LatencyContextRegistrationOptions>? registrationOptions = null)
    {
        var latencyContextRegistrationOptions = registrationOptions != null ? registrationOptions.Value : new LatencyContextRegistrationOptions();
        var throwOnUnregisteredNames = latencyContextOptions.Value.ThrowOnUnregisteredNames;

        CheckpointNameRegistry = CreateRegistry(latencyContextRegistrationOptions.CheckpointNames, throwOnUnregisteredNames);
        TagNameRegistry = CreateRegistry(latencyContextRegistrationOptions.TagNames, throwOnUnregisteredNames);
        MeasureNameRegistry = CreateRegistry(latencyContextRegistrationOptions.MeasureNames, throwOnUnregisteredNames);
    }

    private static Registry CreateRegistry(IEnumerable<string> names, bool throwOnUnregisteredNames)
    {
        var n = GetRegistryKeys(names);
        return new Registry(n, throwOnUnregisteredNames);
    }

    private static string[] GetRegistryKeys(IEnumerable<string> names)
    {
        _ = Throw.IfNull(names);

        foreach (var name in names)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Throw.ArgumentException(nameof(names), "Found null or whitespace name in supplied set");
            }
        }

        HashSet<string> keys = new HashSet<string>(names);
        return keys.ToArray();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection.Pools;

/// <summary>
/// Contains configuration for pools.
/// </summary>
[Experimental]
public sealed class PoolOptions
{
    /// <summary>
    /// Gets or sets the maximal capacity of the pool.
    /// </summary>
    /// <remarks>The default is 1024.</remarks>
    public int Capacity { get; set; } = 1024;
}

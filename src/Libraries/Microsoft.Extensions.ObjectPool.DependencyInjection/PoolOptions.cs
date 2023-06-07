// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// Contains configuration for pools.
/// </summary>
[Experimental]
public sealed class PoolOptions
{
    internal static readonly int DefaultCapacity = Environment.ProcessorCount * 2;

    /// <summary>
    /// Gets or sets the maximal capacity of the pool.
    /// </summary>
    /// <remarks>The default is <c>Environment.ProcessorCount * 2</c>.</remarks>
    public int Capacity { get; set; } = DefaultCapacity;
}

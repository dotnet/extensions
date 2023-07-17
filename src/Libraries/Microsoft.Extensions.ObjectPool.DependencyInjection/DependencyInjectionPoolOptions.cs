// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.ObjectPool;

/// <summary>
/// Contains configuration for pools.
/// </summary>
[Experimental(diagnosticId: Experiments.ObjectPool, UrlFormat = Experiments.UrlFormat)]
public sealed class DependencyInjectionPoolOptions
{
    internal const int DefaultCapacity = 1024;

    /// <summary>
    /// Gets or sets the maximal capacity of the pool.
    /// </summary>
    /// <value>
    /// The default is 1024.
    /// </value>
    public int Capacity { get; set; } = DefaultCapacity;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// The interface of a registry class implementation for custom defined objects
/// registration and retrieval.
/// </summary>
internal interface ICustomResultRegistry
{
    /// <summary>
    /// Gets a custom defined object from the registry by key.
    /// </summary>
    /// <param name="key">The identifier for a registered custom defined object instance.</param>
    /// <returns>
    /// The registered custom defined object instance identified by the given key.
    /// </returns>
    public object GetCustomResult(string key);
}

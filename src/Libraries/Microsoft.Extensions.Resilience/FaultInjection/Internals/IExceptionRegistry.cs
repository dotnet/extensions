// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// The interface of a registry class implementation for exception instances
/// registration and retrieval.
/// </summary>
internal interface IExceptionRegistry
{
    /// <summary>
    /// Gets an exception from the registry by key.
    /// </summary>
    /// <param name="key">The identifier for a registered exception instance.</param>
    /// <returns>
    /// The registered exception instance identified by the given key.
    /// Returns an instance of <see cref="InjectedFaultException"/> by
    /// default if no exception instance with the given key is found.
    /// </returns>
    public Exception GetException(string key);
}

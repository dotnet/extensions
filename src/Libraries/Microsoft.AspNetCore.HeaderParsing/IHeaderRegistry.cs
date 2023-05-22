// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Provides typed header values.
/// </summary>
public interface IHeaderRegistry
{
    /// <summary>
    /// Registers a header parser and returns an object to let you read the header's value at runtime.
    /// </summary>
    /// <typeparam name="T">The type of the header.</typeparam>
    /// <param name="setup">The header setup.</param>
    /// <remarks>If the header already exists, the current instance is returned.</remarks>
    /// <returns>An <see cref="HeaderKey{T}"/> instance.</returns>
    HeaderKey<T> Register<T>(HeaderSetup<T> setup)
        where T : notnull;
}

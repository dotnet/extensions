// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AsyncState;

/// <summary>
/// Provides access to the current async context.
/// Some implementations of this interface may not be thread safe.
/// </summary>
/// <typeparam name="T">The type of the asynchronous state.</typeparam>
[SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Getter and setter throw exceptions.")]
public interface IAsyncContext<T>
    where T : notnull
{
    /// <summary>
    /// Gets current async context.
    /// </summary>
    /// <returns>Current async context.</returns>
    /// <exception cref="InvalidOperationException">Context is not initialized.</exception>
    /// <remarks>
    /// If you are getting an exception that context is not initialized, make sure that you initialized it before usage in your framework.
    /// Also check if you are accessing the context from the current asynchronous flow, starting with context initialization.
    /// </remarks>
    T? Get();

    /// <summary>
    /// Sets async context.
    /// </summary>
    /// <param name="context">Context to be set.</param>
    /// <exception cref="InvalidOperationException">Context is not initialized.</exception>
    /// <remarks>
    /// If you are getting an exception that context is not initialized, make sure that you initialized it before usage in your framework.
    /// Also check if you are accessing the context from the current asynchronous flow, starting with context initialization.
    /// </remarks>
    void Set(T? context);

    /// <summary>
    /// Tries to get the current async context.
    /// </summary>
    /// <param name="context">Receives the context.</param>
    /// <returns><see langword="true"/> if the context is initialized; otherwise, <see langword="false"/>.</returns>
    bool TryGet([MaybeNullWhen(false)] out T? context);
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Microsoft.Extensions.AsyncState;

/// <summary>
/// Provides access to the current async context stored outside of the HTTP pipeline.
/// Some implementations of this interface may not be thread safe.
/// </summary>
/// <typeparam name="T">The type of the asynchronous state.</typeparam>
/// <remarks>This type is intended for internal use. Use <see cref="IAsyncContext{T}"/> instead.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
#pragma warning disable S4023 // Interfaces should not be empty
public interface IAsyncLocalContext<T> : IAsyncContext<T>
#pragma warning restore S4023 // Interfaces should not be empty
    where T : class
{
}

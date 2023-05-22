// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Cloud.DocumentDb;

/// <summary>
/// The operation used in <see cref="BatchItem{T}"/> to indicate the action to perform.
/// </summary>
public enum BatchOperation
{
    /// <summary>
    /// Create item.
    /// </summary>
    Create,

    /// <summary>
    /// Read item.
    /// </summary>
    Read,

    /// <summary>
    /// Replace item.
    /// </summary>
    Replace,

    /// <summary>
    /// Delete item.
    /// </summary>
    Delete,

    /// <summary>
    /// Upsert item.
    /// </summary>
    Upsert,
}

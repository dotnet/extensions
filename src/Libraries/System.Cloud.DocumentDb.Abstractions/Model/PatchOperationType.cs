// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Cloud.DocumentDb;

/// <summary>
/// Enum representing patch operation type.
/// </summary>
public enum PatchOperationType
{
    /// <summary>
    /// Represents add operation.
    /// </summary>
    Add,

    /// <summary>
    /// Represents remove operation.
    /// </summary>
    Remove,

    /// <summary>
    /// Represents replace operation.
    /// </summary>
    Replace,

    /// <summary>
    /// Represents set operation.
    /// </summary>
    Set,

    /// <summary>
    /// Represents increment operation.
    /// </summary>
    Increment
}

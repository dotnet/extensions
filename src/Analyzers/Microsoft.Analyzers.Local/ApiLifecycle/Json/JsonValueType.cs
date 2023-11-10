// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Forked from StyleCop.Analyzers repo.

namespace Microsoft.Extensions.LocalAnalyzers.Json;

/// <summary>
/// Enumerates the types of Json values.
/// </summary>
internal enum JsonValueType
{
    /// <summary>
    /// A <see langword="null" /> value.
    /// </summary>
    Null = 0,

    /// <summary>
    /// A boolean value.
    /// </summary>
    Boolean,

    /// <summary>
    /// A number value.
    /// </summary>
    Number,

    /// <summary>
    /// A string value.
    /// </summary>
    String,

    /// <summary>
    /// An object value.
    /// </summary>
    Object,

    /// <summary>
    /// An array value.
    /// </summary>
    Array,
}

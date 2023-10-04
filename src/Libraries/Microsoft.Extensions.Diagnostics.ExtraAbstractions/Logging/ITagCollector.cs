// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Frozen;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Logging;

/// <summary>
/// Represents a custom tag provider and enables it to emit tags.
/// </summary>
/// <remarks>
/// See <see cref="TagProviderAttribute"/> for details on how this interface is used.
/// </remarks>
public interface ITagCollector
{
    /// <summary>
    /// Adds a tag.
    /// </summary>
    /// <param name="tagName">The name of the tag to add.</param>
    /// <param name="tagValue">The value of the tag to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tagName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="tagName" /> is empty or contains exclusively whitespace,
    /// or a tag of the same name has already been added.
    /// </exception>
    void Add(string tagName, object? tagValue);

    /// <summary>
    /// Adds a tag.
    /// </summary>
    /// <param name="tagName">The name of the tag to add.</param>
    /// <param name="tagValue">The value of the tag to add.</param>
    /// <param name="classificationSet">The data classification set of the tag value.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tagName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="tagName" /> is empty or contains exclusively whitespace,
    /// or a tag of the same name has already been added.
    /// </exception>
    void Add(string tagName, object? tagValue, FrozenSet<DataClassification> classificationSet);
}

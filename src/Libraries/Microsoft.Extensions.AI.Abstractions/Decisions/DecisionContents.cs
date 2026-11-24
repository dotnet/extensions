// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides helpers for owned decision content snapshots.</summary>
/// <remarks>
/// Snapshots are exposed as <see cref="IList{T}"/> to match the existing <see cref="AIContent"/> list contract,
/// but are read-only. The list is owned; the <see cref="AIContent"/> items themselves are shared.
/// </remarks>
internal static class DecisionContents
{
    private static IList<AIContent> Empty { get; } = Array.AsReadOnly(Array.Empty<AIContent>());

    internal static IList<AIContent> FromText(string? text) =>
        string.IsNullOrWhiteSpace(text) ? Empty : Array.AsReadOnly(new AIContent[] { new TextContent(text) });

    internal static IList<AIContent> Snapshot(IEnumerable<AIContent>? contents, string paramName)
    {
        if (contents is null)
        {
            return Empty;
        }

        AIContent[] copy = contents.ToArray();
        if (Array.Exists(copy, static content => content is null))
        {
            Throw.ArgumentException(paramName, "Content items must not be null.");
        }

        return copy.Length == 0 ? Empty : Array.AsReadOnly(copy);
    }

    internal static IList<AIContent> SnapshotRequired(IEnumerable<AIContent>? contents, string paramName)
    {
        _ = Throw.IfNull(contents, paramName);
        IList<AIContent> copy = Snapshot(contents, paramName);
        if (copy.Count == 0)
        {
            Throw.ArgumentException(paramName, "Content must be nonempty.");
        }

        return copy;
    }

    internal static IList<AIContent>? SnapshotOptional(IEnumerable<AIContent>? contents, string paramName)
    {
        if (contents is null)
        {
            return null;
        }

        IList<AIContent> copy = Snapshot(contents, paramName);
        if (copy.Count == 0)
        {
            Throw.ArgumentException(paramName, "Content must be nonempty when specified.");
        }

        return copy;
    }

    internal static IList<AIContent>? FromOptionalText(string? text, string paramName)
    {
        if (text is null)
        {
            return null;
        }

        _ = Throw.IfNullOrWhitespace(text, paramName);
        return FromText(text);
    }
}
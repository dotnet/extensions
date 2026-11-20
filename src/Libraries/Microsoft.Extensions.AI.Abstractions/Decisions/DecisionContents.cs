// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides helpers for owned decision content snapshots.</summary>
internal static class DecisionContents
{
    internal static IReadOnlyList<AIContent> Empty { get; } = Array.AsReadOnly(Array.Empty<AIContent>());

    internal static IReadOnlyList<AIContent> FromText(string? text) =>
        string.IsNullOrWhiteSpace(text) ? Empty : Array.AsReadOnly(new AIContent[] { new TextContent(text) });

    internal static IReadOnlyList<AIContent> Snapshot(IEnumerable<AIContent>? contents, string paramName)
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

    internal static IReadOnlyList<AIContent> SnapshotRequired(IEnumerable<AIContent>? contents, string paramName)
    {
        _ = Throw.IfNull(contents, paramName);
        IReadOnlyList<AIContent> copy = Snapshot(contents, paramName);
        if (copy.Count == 0)
        {
            Throw.ArgumentException(paramName, "Content must be nonempty.");
        }

        return copy;
    }

    internal static IReadOnlyList<AIContent>? SnapshotOptional(IEnumerable<AIContent>? contents, string paramName)
    {
        if (contents is null)
        {
            return null;
        }

        IReadOnlyList<AIContent> copy = Snapshot(contents, paramName);
        if (copy.Count == 0)
        {
            Throw.ArgumentException(paramName, "Content must be nonempty when specified.");
        }

        return copy;
    }

    internal static IReadOnlyList<AIContent>? FromOptionalText(string? text, string paramName)
    {
        if (text is null)
        {
            return null;
        }

        _ = Throw.IfNullOrWhitespace(text, paramName);
        return FromText(text);
    }
}

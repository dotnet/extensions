// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.AI;

/// <summary>Represents a location within source material used for an annotation.</summary>
public class CitationSourceLocation
{
    /// <summary>Gets or sets the page at which the cited material begins.</summary>
    public int? PageStart { get; set; }

    /// <summary>Gets or sets the page at which the cited material ends.</summary>
    public int? PageEnd { get; set; }

    /// <summary>Gets or sets the block or chunk at which the cited material begins.</summary>
    public int? BlockStart { get; set; }

    /// <summary>Gets or sets the block or chunk at which the cited material ends.</summary>
    public int? BlockEnd { get; set; }

    /// <summary>Gets or sets the character at which the cited material begins.</summary>
    public int? CharacterStart { get; set; }

    /// <summary>Gets or sets the character at which the cited material ends.</summary>
    public int? CharacterEnd { get; set; }
}

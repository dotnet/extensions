// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents usage information associated with a chat request and response.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class UsageContent : AIContent
{
    /// <summary>Usage information.</summary>
    private UsageDetails _details;

    /// <summary>Initializes a new instance of the <see cref="UsageContent"/> class with an empty <see cref="UsageDetails"/>.</summary>
    public UsageContent()
    {
        _details = new();
    }

    /// <summary>Initializes a new instance of the <see cref="UsageContent"/> class with the specified <see cref="UsageDetails"/> instance.</summary>
    /// <param name="details">The usage details to store in this content.</param>
    [JsonConstructor]
    public UsageContent(UsageDetails details)
    {
        _details = Throw.IfNull(details);
    }

    /// <summary>Gets or sets the usage information.</summary>
    public UsageDetails Details
    {
        get => _details;
        set => _details = Throw.IfNull(value);
    }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => _details.DebuggerDisplay;
}

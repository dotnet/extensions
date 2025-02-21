// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an error content.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class ErrorContent : AIContent
{
    /// <summary>Initializes a new instance of the <see cref="ErrorContent"/> class with the specified message.</summary>
    /// <param name="message">The message to store in this content.</param>
    [JsonConstructor]
    public ErrorContent(string message)
    {
        Message = Throw.IfNull(message);
    }

    /// <summary>Gets or sets the error message.</summary>
    public string Message { get; set; }

    /// <summary>Gets or sets the error code.</summary>
    public string? Code { get; set; }

    /// <summary>Gets or sets the error details.</summary>
    public string? Details { get; set; }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get
        {
            string display = $"Message = {Message} ";

            display += Code is not null ?
                $", Code = {Code}" : string.Empty;

            display += Details is not null ?
                $", Details = {Details}" : string.Empty;

            return display;
        }
    }
}

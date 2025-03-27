// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents an error.</summary>
/// <remarks>
/// Typically, <see cref="ErrorContent"/> is used for non-fatal errors, where something went wrong
/// as part of the operation but the operation was still able to continue.
/// </remarks>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public class ErrorContent : AIContent
{
    /// <summary>The error message.</summary>
    private string _message;

    /// <summary>Initializes a new instance of the <see cref="ErrorContent"/> class with the specified message.</summary>
    /// <param name="message">The message to store in this content.</param>
    [JsonConstructor]
    public ErrorContent(string message)
    {
        _message = Throw.IfNull(message);
    }

    /// <summary>Gets or sets the error message.</summary>
    public string Message
    {
        get => _message;
        set => _message = Throw.IfNull(value);
    }

    /// <summary>Gets or sets the error code.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Gets or sets the error details.</summary>
    public string? Details { get; set; }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        $"Error = {Message}" +
        (ErrorCode is not null ? $" ({ErrorCode})" : string.Empty) +
        (Details is not null ? $" - {Details}" : string.Empty);
}

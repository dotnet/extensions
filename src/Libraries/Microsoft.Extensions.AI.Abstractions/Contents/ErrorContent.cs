// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
    private string? _message;

    /// <summary>Initializes a new instance of the <see cref="ErrorContent"/> class with the specified error message.</summary>
    /// <param name="message">The error message to store in this content.</param>
    public ErrorContent(string? message)
    {
        _message = message;
    }

    /// <summary>Gets or sets the error message.</summary>
    [AllowNull]
    public string Message
    {
        get => _message ?? string.Empty;
        set => _message = value;
    }

    /// <summary>Gets or sets an error code associated with the error.</summary>
    public string? ErrorCode { get; set; }

    /// <summary>Gets or sets additional details about the error.</summary>
    public string? Details { get; set; }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay =>
        $"Error = \"{Message}\"" +
        (!string.IsNullOrWhiteSpace(ErrorCode) ? $" ({ErrorCode})" : string.Empty) +
        (!string.IsNullOrWhiteSpace(Details) ? $" - \"{Details}\"" : string.Empty);
}

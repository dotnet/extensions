// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a mode where a chat tool must be called. This class can optionally nominate a specific function
/// or indicate that any of the functions can be selected.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class RequiredChatToolMode : ChatToolMode
{
    /// <summary>
    /// Gets the name of a specific <see cref="AIFunction"/> that must be called.
    /// </summary>
    /// <remarks>
    /// If the value is <see langword="null"/>, any available function can be selected (but at least one must be).
    /// </remarks>
    public string? RequiredFunctionName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredChatToolMode"/> class that requires a specific function to be called.
    /// </summary>
    /// <param name="requiredFunctionName">The name of the function that must be called.</param>
    /// <remarks>
    /// <paramref name="requiredFunctionName"/> can be <see langword="null"/>. However, it's preferable to use
    /// <see cref="ChatToolMode.RequireAny"/> when any function can be selected.
    /// </remarks>
    public RequiredChatToolMode(string? requiredFunctionName)
    {
        if (requiredFunctionName is not null)
        {
            _ = Throw.IfNullOrWhitespace(requiredFunctionName);
        }

        RequiredFunctionName = requiredFunctionName;
    }

    // The reason for not overriding Equals/GetHashCode (e.g., so two instances are equal if they
    // have the same RequiredFunctionName) is to leave open the option to unseal the type in the
    // future. If we did define equality based on RequiredFunctionName but a subclass added further
    // fields, this would lead to wrong behavior unless the subclass author remembers to re-override
    // Equals/GetHashCode as well, which they likely won't.

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    private string DebuggerDisplay => $"Required: {RequiredFunctionName ?? "Any"}";

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is RequiredChatToolMode other &&
        RequiredFunctionName == other.RequiredFunctionName;

    /// <inheritdoc/>
    public override int GetHashCode() =>
        RequiredFunctionName?.GetHashCode(StringComparison.Ordinal) ??
        typeof(RequiredChatToolMode).GetHashCode();
}

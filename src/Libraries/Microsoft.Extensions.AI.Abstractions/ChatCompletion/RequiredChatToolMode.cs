// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents a mode where a chat tool must be called. This class can optionally nominate a specific tool
/// or indicate that any of the tools can be selected.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public sealed class RequiredChatToolMode : ChatToolMode
{
    /// <summary>
    /// Gets the name of a specific function tool that must be called.
    /// </summary>
    /// <remarks>
    /// If both <see cref="RequiredFunctionName"/> and <see cref="RequiredTool"/> are <see langword="null"/>,
    /// any available tool can be selected (but at least one must be).
    /// </remarks>
    public string? RequiredFunctionName { get; }

    /// <summary>Gets the specific tool that must be called.</summary>
    /// <remarks>
    /// <para>
    /// If both <see cref="RequiredFunctionName"/> and <see cref="RequiredTool"/> are <see langword="null"/>,
    /// any available tool can be selected (but at least one must be).
    /// </para>
    /// <para>
    /// Note that <see cref="RequiredTool"/> will not serialize to JSON as part of serializing
    /// the <see cref="RequiredChatToolMode"/> instance, just as <see cref="ChatOptions.Tools"/> doesn't serialize. As such, attempting to
    /// roundtrip a <see cref="RequiredChatToolMode"/> through JSON serialization may lead to the deserialized instance having <see cref="RequiredTool"/>
    /// set to <see langword="null"/>.
    /// </para>
    /// </remarks>
    [JsonIgnore]
    public AITool? RequiredTool { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredChatToolMode"/> class that requires a specific tool to be called.
    /// </summary>
    /// <param name="requiredFunctionName">The name of the function that must be called.</param>
    /// <exception cref="ArgumentException"><paramref name="requiredFunctionName"/> is empty or composed entirely of whitespace.</exception>
    /// <remarks>
    /// <para>
    /// <paramref name="requiredFunctionName"/> can be <see langword="null"/>. However, it's preferable to use
    /// <see cref="ChatToolMode.RequireAny"/> when any function can be selected.
    /// </para>
    /// <para>
    /// The specified tool must also be included in the list of tools provided in the request,
    /// such as via <see cref="ChatOptions.Tools"/>.
    /// </para>
    /// </remarks>
    [JsonConstructor]
    public RequiredChatToolMode(string? requiredFunctionName)
    {
        if (requiredFunctionName is not null)
        {
            _ = Throw.IfNullOrWhitespace(requiredFunctionName);
        }

        RequiredFunctionName = requiredFunctionName;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiredChatToolMode"/> class that requires a specific tool to be called.
    /// </summary>
    /// <param name="requiredTool">The specific tool that must be called.</param>
    /// <para>
    /// <paramref name="requiredTool"/> can be <see langword="null"/>. However, it's preferable to use
    /// <see cref="ChatToolMode.RequireAny"/> when any function can be selected.
    /// </para>
    /// <para>
    /// Specifying a <paramref name="requiredTool"/> in a <see cref="RequiredChatToolMode"/> stored
    /// into <see cref="ChatOptions.ToolMode"/> does not automatically include that tool in <see cref="ChatOptions.Tools"/>.
    /// The tool must still be provided separately from the <see cref="ChatOptions.ToolMode"/>.
    /// </para>
    public RequiredChatToolMode(AITool? requiredTool)
    {
        if (requiredTool is not null)
        {
            RequiredTool = requiredTool;
            RequiredFunctionName = requiredTool is AIFunctionDeclaration af ? af.Name : null;
        }
    }

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay => $"Required: {RequiredFunctionName ?? RequiredTool?.Name ?? "Any"}";

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is RequiredChatToolMode other &&
        (RequiredFunctionName is not null || other.RequiredFunctionName is not null ?
            RequiredFunctionName == other.RequiredFunctionName :
            Equals(RequiredTool, other.RequiredTool));

    /// <inheritdoc/>
    public override int GetHashCode() =>
        RequiredFunctionName?.GetHashCode(StringComparison.Ordinal) ??
        RequiredTool?.GetHashCode() ??
        typeof(RequiredChatToolMode).GetHashCode();
}

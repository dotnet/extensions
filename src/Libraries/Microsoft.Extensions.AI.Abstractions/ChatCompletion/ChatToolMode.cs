﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Describes how tools should be selected by a <see cref="IChatClient"/>.
/// </summary>
/// <remarks>
/// The predefined values <see cref="Auto" /> and <see cref="RequireAny"/> are provided.
/// To nominate a specific function, use <see cref="RequireSpecific(string)"/>.
/// </remarks>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(AutoChatToolMode), typeDiscriminator: "auto")]
[JsonDerivedType(typeof(RequiredChatToolMode), typeDiscriminator: "required")]
#pragma warning disable CA1052 // Static holder types should be Static or NotInheritable
public class ChatToolMode
#pragma warning restore CA1052
{
    /// <summary>Initializes a new instance of the <see cref="ChatToolMode"/> class.</summary>
    /// <remarks>Prevents external instantiation. Close the inheritance hierarchy for now until we have good reason to open it.</remarks>
    private protected ChatToolMode()
    {
    }

    /// <summary>
    /// Gets a predefined <see cref="ChatToolMode"/> indicating that tool usage is optional.
    /// </summary>
    /// <remarks>
    /// <see cref="ChatOptions.Tools"/> can contain zero or more <see cref="AITool"/>
    /// instances, and the <see cref="IChatClient"/> is free to invoke zero or more of them.
    /// </remarks>
    public static AutoChatToolMode Auto { get; } = new AutoChatToolMode();

    /// <summary>
    /// Gets a predefined <see cref="ChatToolMode"/> indicating that tool usage is required,
    /// but that any tool can be selected. At least one tool must be provided in <see cref="ChatOptions.Tools"/>.
    /// </summary>
    public static RequiredChatToolMode RequireAny { get; } = new(requiredFunctionName: null);

    /// <summary>
    /// Instantiates a <see cref="ChatToolMode"/> indicating that tool usage is required,
    /// and that the specified <see cref="AIFunction"/> must be selected. The function name
    /// must match an entry in <see cref="ChatOptions.Tools"/>.
    /// </summary>
    /// <param name="functionName">The name of the required function.</param>
    /// <returns>An instance of <see cref="RequiredChatToolMode"/> for the specified function name.</returns>
    public static RequiredChatToolMode RequireSpecific(string functionName) => new(functionName);
}

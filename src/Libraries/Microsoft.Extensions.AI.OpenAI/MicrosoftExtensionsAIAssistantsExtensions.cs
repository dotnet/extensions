﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace OpenAI.Assistants;

/// <summary>Provides extension methods for working with content associated with OpenAI.Assistants.</summary>
public static class MicrosoftExtensionsAIAssistantsExtensions
{
    /// <summary>Creates an OpenAI <see cref="FunctionToolDefinition"/> from an <see cref="AIFunction"/>.</summary>
    /// <param name="function">The function to convert.</param>
    /// <returns>An OpenAI <see cref="FunctionToolDefinition"/> representing <paramref name="function"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    public static FunctionToolDefinition AsOpenAIAssistantsFunctionToolDefinition(this AIFunction function) =>
        OpenAIAssistantsChatClient.ToOpenAIAssistantsFunctionToolDefinition(Throw.IfNull(function));
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace OpenAI.Realtime;

/// <summary>Provides extension methods for working with content associated with OpenAI.Realtime.</summary>
[Experimental(DiagnosticIds.Experiments.AIOpenAIRealtime)]
public static class MicrosoftExtensionsAIRealtimeExtensions
{
    /// <summary>Creates an OpenAI <see cref="RealtimeFunctionTool"/> from an <see cref="AIFunctionDeclaration"/>.</summary>
    /// <param name="function">The function to convert.</param>
    /// <returns>An OpenAI <see cref="RealtimeFunctionTool"/> representing <paramref name="function"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    public static RealtimeFunctionTool AsOpenAIRealtimeFunctionTool(this AIFunctionDeclaration function) =>
        OpenAIRealtimeConversationClient.ToOpenAIRealtimeFunctionTool(Throw.IfNull(function));
}

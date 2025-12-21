// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Extension methods for adding tool reduction middleware to a chat client pipeline.</summary>
[Experimental(DiagnosticIds.Experiments.ToolReduction, UrlFormat = DiagnosticIds.UrlFormat)]
public static class ChatClientBuilderToolReductionExtensions
{
    /// <summary>
    /// Adds tool reduction to the chat client pipeline using the specified <paramref name="strategy"/>.
    /// </summary>
    /// <param name="builder">The chat client builder.</param>
    /// <param name="strategy">The reduction strategy.</param>
    /// <returns>The original builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="builder"/> or <paramref name="strategy"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This should typically appear in the pipeline before function invocation middleware so that only the reduced tools
    /// are exposed to the underlying provider.
    /// </remarks>
    public static ChatClientBuilder UseToolReduction(this ChatClientBuilder builder, IToolReductionStrategy strategy)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(strategy);

        return builder.Use(inner => new ToolReducingChatClient(inner, strategy));
    }
}

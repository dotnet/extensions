// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for attaching a <see cref="ReducingChatClient"/> to a chat pipeline.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIChatReduction, UrlFormat = DiagnosticIds.UrlFormat)]
public static class ReducingChatClientBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="ReducingChatClient"/> to the chat pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/> being used to build the chat pipeline.</param>
    /// <param name="reducer">An optional <see cref="IChatReducer"/> to apply to the chat client. If not supplied, an instance will be resolved from the service provider.</param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="ReducingChatClient"/> instance.</param>
    /// <returns>The configured <see cref="ChatClientBuilder"/> instance.</returns>
    public static ChatClientBuilder UseChatReducer(
        this ChatClientBuilder builder,
        IChatReducer? reducer = null,
        Action<ReducingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            reducer ??= services.GetRequiredService<IChatReducer>();

            var chatClient = new ReducingChatClient(innerClient, reducer);
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}

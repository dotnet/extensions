// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for adding <see cref="HostedConversationChatClient"/> to a <see cref="ChatClientBuilder"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
public static class HostedConversationChatClientBuilderExtensions
{
    /// <summary>
    /// Adds a <see cref="HostedConversationChatClient"/> to the chat client pipeline, making an <see cref="IHostedConversationClient"/> discoverable via
    /// <see cref="IChatClient.GetService(Type, object?)"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="hostedConversationClient">The <see cref="IHostedConversationClient"/> to make discoverable.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ChatClientBuilder UseHostedConversations(
        this ChatClientBuilder builder,
        IHostedConversationClient hostedConversationClient)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(hostedConversationClient);

        return builder.Use(innerClient => new HostedConversationChatClient(innerClient, hostedConversationClient));
    }

    /// <summary>
    /// Adds a <see cref="HostedConversationChatClient"/> to the chat client pipeline, making an <see cref="IHostedConversationClient"/> discoverable via
    /// <see cref="IChatClient.GetService(Type, object?)"/>. The <see cref="IHostedConversationClient"/> is resolved from the service provider.
    /// </summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ChatClientBuilder UseHostedConversations(
        this ChatClientBuilder builder)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
            new HostedConversationChatClient(innerClient, services.GetRequiredService<IHostedConversationClient>()));
    }
}

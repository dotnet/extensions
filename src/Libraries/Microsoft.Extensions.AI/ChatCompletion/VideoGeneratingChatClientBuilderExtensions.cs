// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="VideoGeneratingChatClient"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public static class VideoGeneratingChatClientBuilderExtensions
{
    /// <summary>Adds video generation capabilities to the chat client pipeline.</summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="videoGenerator">
    /// An optional <see cref="IVideoGenerator"/> used for video generation operations.
    /// If not supplied, a required instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="VideoGeneratingChatClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method enables the chat client to handle <see cref="HostedVideoGenerationTool"/> instances by converting them
    /// into function tools that can be invoked by the underlying chat model to perform video generation and editing operations.
    /// </para>
    /// </remarks>
    public static ChatClientBuilder UseVideoGeneration(
        this ChatClientBuilder builder,
        IVideoGenerator? videoGenerator = null,
        Action<VideoGeneratingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            videoGenerator ??= services.GetRequiredService<IVideoGenerator>();

            var chatClient = new VideoGeneratingChatClient(innerClient, videoGenerator);
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}

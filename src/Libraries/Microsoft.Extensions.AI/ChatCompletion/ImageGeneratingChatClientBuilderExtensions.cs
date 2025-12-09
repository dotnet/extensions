// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ImageGeneratingChatClient"/> instances.</summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.ImageGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public static class ImageGeneratingChatClientBuilderExtensions
{
    /// <summary>Adds image generation capabilities to the chat client pipeline.</summary>
    /// <param name="builder">The <see cref="ChatClientBuilder"/>.</param>
    /// <param name="imageGenerator">
    /// An optional <see cref="IImageGenerator"/> used for image generation operations.
    /// If not supplied, a required instance will be resolved from the service provider.
    /// </param>
    /// <param name="configure">An optional callback that can be used to configure the <see cref="ImageGeneratingChatClient"/> instance.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method enables the chat client to handle <see cref="HostedImageGenerationTool"/> instances by converting them
    /// into function tools that can be invoked by the underlying chat model to perform image generation and editing operations.
    /// </para>
    /// </remarks>
    public static ChatClientBuilder UseImageGeneration(
        this ChatClientBuilder builder,
        IImageGenerator? imageGenerator = null,
        Action<ImageGeneratingChatClient>? configure = null)
    {
        _ = Throw.IfNull(builder);

        return builder.Use((innerClient, services) =>
        {
            imageGenerator ??= services.GetRequiredService<IImageGenerator>();

            var chatClient = new ImageGeneratingChatClient(innerClient, imageGenerator);
            configure?.Invoke(chatClient);
            return chatClient;
        });
    }
}

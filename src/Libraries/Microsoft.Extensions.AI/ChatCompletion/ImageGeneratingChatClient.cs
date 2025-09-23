// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Tools;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating chat client that enables image generation capabilities by converting <see cref="HostedImageGenerationTool"/> instances to function tools.</summary>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the
/// <see cref="IImageGenerator"/> employed is also thread-safe for concurrent use.
/// </para>
/// <para>
/// This client automatically detects <see cref="HostedImageGenerationTool"/> instances in the <see cref="ChatOptions.Tools"/> collection
/// and replaces them with equivalent function tools that the chat client can invoke to perform image generation and editing operations.
/// </para>
/// </remarks>
[Experimental("MEAI001")]
public sealed class ImageGeneratingChatClient : DelegatingChatClient
{
    private readonly IImageGenerator _imageGenerator;
    private readonly AITool[] _aiTools;

    /// <summary>Stores generated image content from function calls to be included in responses.</summary>
    private List<AIContent>? _generatedImageContent;

    /// <summary>Initializes a new instance of the <see cref="ImageGeneratingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="imageGenerator">An <see cref="IImageGenerator"/> instance that will be used for image generation operations.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> or <paramref name="imageGenerator"/> is <see langword="null"/>.</exception>
    public ImageGeneratingChatClient(IChatClient innerClient, IImageGenerator imageGenerator)
        : base(innerClient)
    {
        _imageGenerator = Throw.IfNull(imageGenerator);
        _aiTools =
        [
            AIFunctionFactory.Create(GenerateImageAsync),
            AIFunctionFactory.Create(EditImageAsync)
        ];
    }

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Clear any existing generated content for this request
        _generatedImageContent = null;

        // Process the chat options to replace HostedImageGenerationTool with functions
        var processedOptions = ProcessChatOptions(options);

        // Get response from base implementation
        var response = await base.GetResponseAsync(messages, processedOptions, cancellationToken);

        // If we have generated image content, add it to the response
        if (_generatedImageContent is { Count: > 0 })
        {
            var lastMessage = response.Messages.LastOrDefault();
            if (lastMessage is not null)
            {
                // Add generated images to the last message
                foreach (var content in _generatedImageContent)
                {
                    lastMessage.Contents.Add(content);
                }
            }

            // Clear the content after using it
            _generatedImageContent = null;
        }

        return response;
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Clear any existing generated content for this request
        _generatedImageContent = null;

        // Process the chat options to replace HostedImageGenerationTool with functions
        var processedOptions = ProcessChatOptions(options);

        await foreach (var update in base.GetStreamingResponseAsync(messages, processedOptions, cancellationToken))
        {
            // Check if we have generated images since the last update and inject them into this update
            if (_generatedImageContent is { Count: > 0 })
            {
                // Add generated images to the current update's contents
                foreach (var content in _generatedImageContent)
                {
                    update.Contents.Add(content);
                }

                // Clear the stored content after using it
                _generatedImageContent.Clear();
            }

            yield return update;
        }
    }

    /// <summary>Provides a mechanism for releasing unmanaged resources.</summary>
    /// <param name="disposing"><see langword="true"/> to dispose managed resources; otherwise, <see langword="false"/>.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _imageGenerator.Dispose();
        }

        base.Dispose(disposing);
    }

    private ChatOptions? ProcessChatOptions(ChatOptions? options)
    {
        if (options?.Tools is null || options.Tools.Count == 0)
        {
            return options;
        }

        if (!options.Tools.Any(tool => tool is HostedImageGenerationTool))
        {
            return options;
        }

        var modifiedOptions = options.Clone();

        // Remove any existing HostedImageGenerationTool instances and add the function tools.
        var tools = new List<AITool>(options.Tools.Count - 1 + _aiTools.Length);
        tools.AddRange(options.Tools.Where(tool => tool is not HostedImageGenerationTool));
        tools.AddRange(_aiTools);

        modifiedOptions.Tools = tools;
        return modifiedOptions;
    }

    [Description("Generates an image based on a text prompt")]
    private async Task<string> GenerateImageAsync(string prompt, ImageGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var request = new ImageGenerationRequest(prompt);
        var response = await _imageGenerator.GenerateAsync(request, options, cancellationToken);

        if (response.Contents.Count == 0)
        {
            return "No image was generated.";
        }

        // Store the generated image content to be included in the response
        (_generatedImageContent ??= []).AddRange(response.Contents);

        var imageCount = response.Contents.Count;
        return $"Generated {imageCount} image(s) based on the prompt: '{prompt}'";
    }

    [Description("Edits an existing image based on a text prompt and original image data")]
    private async Task<string> EditImageAsync(string prompt, string imageData, ImageGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var imageBytes = Convert.FromBase64String(imageData);
            var originalImage = new DataContent(imageBytes, "image/png");

            var request = new ImageGenerationRequest(prompt, [originalImage]);
            var response = await _imageGenerator.GenerateAsync(request, options, cancellationToken);

            if (response.Contents.Count == 0)
            {
                return "No edited image was generated.";
            }

            // Store the generated image content to be included in the response
            (_generatedImageContent ??= []).AddRange(response.Contents);

            var imageCount = response.Contents.Count;
            return $"Edited {imageCount} image(s) based on the prompt: '{prompt}'";
        }
        catch (FormatException)
        {
            return "Invalid image data format. Please provide a valid base64-encoded image.";
        }
    }
}

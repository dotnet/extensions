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
    private readonly HashSet<string> _functionNames;

    /// <summary>Stores mapping of function call IDs to generated image content.</summary>
    private readonly Dictionary<string, List<AIContent>> _imageContentByCallId = [];
    private ImageGenerationOptions? _imageGenerationOptions;

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

        _functionNames = new(_aiTools.Select(t => t.Name), StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Clear any existing generated content for this request
        _imageContentByCallId.Clear();

        // Process the chat options to replace HostedImageGenerationTool with functions
        var processedOptions = ProcessChatOptions(options);

        // Get response from base implementation
        var response = await base.GetResponseAsync(messages, processedOptions, cancellationToken);

        // Replace FunctionResultContent instances with generated image content
        foreach (var message in response.Messages)
        {
            var newContents = ReplaceImageGenerationFunctionResults(message.Contents);
            message.Contents = newContents;
        }

        return response;
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Clear any existing generated content for this request
        _imageContentByCallId.Clear();

        // Process the chat options to replace HostedImageGenerationTool with functions
        var processedOptions = ProcessChatOptions(options);

        await foreach (var update in base.GetStreamingResponseAsync(messages, processedOptions, cancellationToken))
        {
            // Replace any FunctionResultContent instances with generated image content
            var newContents = ReplaceImageGenerationFunctionResults(update.Contents);

            if (newContents != update.Contents)
            {
                // Create a new update instance with modified contents
                var modifiedUpdate = new ChatResponseUpdate(update.Role, newContents)
                {
                    AuthorName = update.AuthorName,
                    RawRepresentation = update.RawRepresentation,
                    AdditionalProperties = update.AdditionalProperties,
                    ResponseId = update.ResponseId,
                    MessageId = update.MessageId,
                    ConversationId = update.ConversationId
                };

                yield return modifiedUpdate;
            }
            else
            {
                yield return update;
            }
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

        var tools = options.Tools;
        ChatOptions? modifiedOptions = null;

        for (int i = 0; i < tools.Count; i++)
        {
            var tool = options.Tools[i];

            // remove all instances of HostedImageGenerationTool and store the options from the last one
            if (tool is HostedImageGenerationTool imageGenerationTool)
            {
                _imageGenerationOptions = imageGenerationTool.Options;

#pragma warning disable S127
                // for the first image generation tool, clone the options and insert our function tools
                // remove any subsequent image generation tools
                if (modifiedOptions is null)
                {
                    modifiedOptions = options.Clone();
                    tools = modifiedOptions.Tools!;

                    tools.RemoveAt(i--);

                    foreach (var functionTool in _aiTools)
                    {
                        tools.Insert(++i, functionTool);
                    }
                }
                else
                {
                    tools.RemoveAt(i--);
                }
#pragma warning restore S127 
            }
        }

        return modifiedOptions ?? options;
    }

    /// <summary>Replaces FunctionResultContent instances for image generation functions with actual generated image content.</summary>
    /// <param name="contents">The list of AI content to process.</param>
    private IList<AIContent> ReplaceImageGenerationFunctionResults(IList<AIContent> contents)
    {
        IList<AIContent>? newContents = null;

#pragma warning disable S127
#pragma warning disable S125
        // Replace FunctionResultContent instances with generated image content
        for (int i = contents.Count - 1; i >= 0; i--)
        {
            var content = contents[i];

            if (content is FunctionCallContent functionCall &&
                _functionNames.Contains(functionCall.Name))
            {
                EnsureNewContents();
                contents.RemoveAt(i--);
            }

            if (content is FunctionResultContent functionResult &&
                _imageContentByCallId.TryGetValue(functionResult.CallId, out var imageContents))
            {
                // Remove the function result
                EnsureNewContents();
                contents.RemoveAt(i);

                // Insert generated image content in its place
                for (int j = imageContents.Count - 1; j >= 0; j--)
                {
                    contents.Insert(i, imageContents[j]);
                }

                _ = _imageContentByCallId.Remove(functionResult.CallId);
            }
        }

        return contents;

        void EnsureNewContents()
        {
            if (newContents is null)
            {
                newContents = [.. contents];
                contents = newContents;
            }
        }
    }
#pragma warning disable EA0014
    [Description("Generates images based on a text description")]
    private async Task<string> GenerateImageAsync(
         [Description("A detailed description of the image to generate")] string prompt)
    {
        // Get the call ID from the current function invocation context
        var callId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
        if (callId == null)
        {
            return "No call ID available for image generation.";
        }

        var request = new ImageGenerationRequest(prompt);
        var options = _imageGenerationOptions ?? new ImageGenerationOptions();
        options.Count ??= 1;

        var response = await _imageGenerator.GenerateAsync(request, options);

        if (response.Contents.Count == 0)
        {
            return "No image was generated.";
        }

        // Store the generated image content mapped to this call ID
        _imageContentByCallId[callId] = [.. response.Contents];

        int imageCount = 0;
        List<string> imageIds = [];

        foreach (var content in response.Contents)
        {
            if (content is DataContent imageContent)
            {
                imageCount++;

                // if there is no name, generate one based on the call ID and index
                imageContent.Name ??= $"{callId}_image_{imageCount}";
                imageIds.Add(imageContent.Name);

                imageContent.AdditionalProperties ??= new();
                imageContent.AdditionalProperties["prompt"] = prompt;
            }
        }

        return $"Generated {imageCount} image(s) with IDs: {string.Join(",", imageIds)} based on the prompt: '{prompt}'";
    }

    [Description("Edits an existing image based on a text description")]
    private async Task<string> EditImageAsync(
        [Description("A detailed description of the image to generate")] string prompt,
        [Description("The original image content to edit")] string imageData)
    {
        // Get the call ID from the current function invocation context
        var callId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
        if (callId == null)
        {
            return "No call ID available for image editing.";
        }

        try
        {
            var imageBytes = Convert.FromBase64String(imageData);
            var originalImage = new DataContent(imageBytes, "image/png");

            var request = new ImageGenerationRequest(prompt, [originalImage]);
            var response = await _imageGenerator.GenerateAsync(request, _imageGenerationOptions);

            if (response.Contents.Count == 0)
            {
                return "No edited image was generated.";
            }

            // Store the generated image content mapped to this call ID
            _imageContentByCallId[callId] = [.. response.Contents];

            var imageCount = response.Contents.Count;
            return $"Edited {imageCount} image(s) based on the prompt: '{prompt}'";
        }
        catch (FormatException)
        {
            return "Invalid image data format. Please provide a valid base64-encoded image.";
        }
    }
#pragma warning restore EA0014
}

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
    /// <summary>
    /// Specifies how image and other data content is handled when passing data to an inner client.
    /// </summary>
    /// <remarks>
    /// Use this enumeration to control whether images in the data content are passed as-is, replaced
    /// with unique identifiers, or only generated images are replaced. This setting affects how downstream clients
    /// receive and process image data.
    /// Reducing what's passed downstream can help manage the context window.
    /// </remarks>
    public enum DataContentHandling
    {
        /// <summary>Pass all DataContent to inner client.</summary>
        None,

        /// <summary>Replace all images with unique identifers when passing to inner client.</summary>
        AllImages,

        /// <summary>Replace only images that were produced by past of image generation requests with unique identifiers when passing to inner client.</summary>
        GeneratedImages
    }

    private const string ImageKey = "meai_image";

    private readonly IImageGenerator _imageGenerator;
    private readonly AITool[] _aiTools;
    private readonly HashSet<string> _functionNames;
    private readonly DataContentHandling _dataContentHandling;

    // the following fields all have scope per-request. They are cleared at the start of each request.
    private readonly Dictionary<string, List<AIContent>> _imageContentByCallId = [];
    private readonly Dictionary<string, AIContent> _imageContentById = new(StringComparer.OrdinalIgnoreCase);
    private ImageGenerationOptions? _imageGenerationOptions;

    private static List<T> CopyList<T>(IList<T> original, int toOffsetExclusive, int additionalCapacity = 0)
    {
        var newList = new List<T>(original.Count + additionalCapacity);

        // Copy all items up to and excluding the current index
        for (int j = 0; j < toOffsetExclusive; j++)
        {
            newList.Add(original[j]);
        }

        return newList;
    }

    /// <summary>Initializes a new instance of the <see cref="ImageGeneratingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="imageGenerator">An <see cref="IImageGenerator"/> instance that will be used for image generation operations.</param>
    /// <param name="dataContentHandling">Specifies how to handle <see cref="DataContent"/> instances when passing messages to the inner client.
    /// The default is <see cref="DataContentHandling.AllImages"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> or <paramref name="imageGenerator"/> is <see langword="null"/>.</exception>
    public ImageGeneratingChatClient(IChatClient innerClient, IImageGenerator imageGenerator, DataContentHandling dataContentHandling = DataContentHandling.AllImages)
        : base(innerClient)
    {
        _imageGenerator = Throw.IfNull(imageGenerator);
        _aiTools =
        [
            AIFunctionFactory.Create(GenerateImageAsync),
            AIFunctionFactory.Create(EditImageAsync),
            AIFunctionFactory.Create(GetImagesForEdit)
        ];

        _functionNames = new(_aiTools.Select(t => t.Name), StringComparer.Ordinal);
        _dataContentHandling = dataContentHandling;
    }

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        // Clear any existing generated content for this request
        _imageContentByCallId.Clear();
        _imageContentById.Clear();

        try
        {
            // Process the chat options to replace HostedImageGenerationTool with functions
            var processedOptions = ProcessChatOptions(options);
            var processedMessages = ProcessChatMessages(messages);

            // Get response from base implementation
            var response = await base.GetResponseAsync(processedMessages, processedOptions, cancellationToken);

            // Replace FunctionResultContent instances with generated image content
            foreach (var message in response.Messages)
            {
                message.Contents = ReplaceImageGenerationFunctionResults(message.Contents);
            }

            return response;
        }
        finally
        {
            // Clear any existing generated content for this request
            _imageContentByCallId.Clear();
            _imageContentById.Clear();
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        // Clear any existing generated content for this request
        _imageContentByCallId.Clear();
        _imageContentById.Clear();

        try
        {
            // Process the chat options to replace HostedImageGenerationTool with functions
            var processedOptions = ProcessChatOptions(options);
            var processedMessages = ProcessChatMessages(messages);

            await foreach (var update in base.GetStreamingResponseAsync(processedMessages, processedOptions, cancellationToken))
            {
                // Replace any FunctionResultContent instances with generated image content
                var newContents = ReplaceImageGenerationFunctionResults(update.Contents);

                if (!ReferenceEquals(newContents, update.Contents))
                {
                    // Create a new update instance with modified contents
                    var modifiedUpdate = update.Clone();
                    modifiedUpdate.Contents = newContents;
                    yield return modifiedUpdate;
                }
                else
                {
                    yield return update;
                }
            }
        }
        finally
        {
            // Clear any existing generated content for this request
            _imageContentByCallId.Clear();
            _imageContentById.Clear();
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

    private IEnumerable<ChatMessage> ProcessChatMessages(IEnumerable<ChatMessage> messages)
    {
        // If no special handling is needed, return the original messages
        if (_dataContentHandling == DataContentHandling.None)
        {
            return messages;
        }

        List<ChatMessage>? newMessages = null;
        int messageIndex = 0;
        foreach (var message in messages)
        {
            List<AIContent>? newContents = null;
            for (int contentIndex = 0; contentIndex < message.Contents.Count; contentIndex++)
            {
                var content = message.Contents[contentIndex];
                if (content is DataContent dataContent && dataContent.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    bool isGeneratedImage = dataContent.AdditionalProperties?.ContainsKey(ImageKey) == true;
                    if (_dataContentHandling == DataContentHandling.AllImages ||
                        (_dataContentHandling == DataContentHandling.GeneratedImages && isGeneratedImage))
                    {
                        // Replace image with a placeholder text content
                        var imageId = StoreImage(dataContent);

                        newContents ??= CopyList(message.Contents, contentIndex);
                        newContents.Add(new TextContent($"[{ImageKey}:{imageId}] available for edit.")
                        {
                            Annotations = dataContent.Annotations,
                            AdditionalProperties = dataContent.AdditionalProperties
                        });
                        continue; // Skip adding the original content
                    }
                }

                // Add the original content if no replacement was made
                newContents?.Add(content);
            }

            if (newContents != null)
            {
                newMessages ??= new List<ChatMessage>(messages.Take(messageIndex));

                var newMessage = message.Clone();

                newMessage.Contents = newContents;
                newMessages.Add(newMessage);
            }
            else
            {
                newMessages?.Add(message);

            }

            messageIndex++;
        }

        return newMessages ?? messages;
    }

    private ChatOptions? ProcessChatOptions(ChatOptions? options)
    {
        if (options?.Tools is null || options.Tools.Count == 0)
        {
            return options;
        }

        List<AITool>? newTools = null;
        var tools = options.Tools;
        for (int i = 0; i < tools.Count; i++)
        {
            var tool = tools[i];

            // remove all instances of HostedImageGenerationTool and store the options from the last one
            if (tool is HostedImageGenerationTool imageGenerationTool)
            {
                _imageGenerationOptions = imageGenerationTool.Options;

                // for the first image generation tool, clone the options and insert our function tools
                // remove any subsequent image generation tools
                if (newTools is null)
                {
                    newTools = CopyList(tools, i);
                    newTools.AddRange(_aiTools);
                }
            }
            else
            {
                newTools?.Add(tool);
            }
        }

        if (newTools is not null)
        {
            var newOptions = options.Clone();
            newOptions.Tools = newTools;
            return newOptions;
        }

        return options;
    }

    private DataContent? RetrieveImageContent(string imageId)
    {
        if (_imageContentById.TryGetValue(imageId, out var imageContent))
        {
            return imageContent as DataContent;
        }

        return null;
    }

    private string StoreImage(DataContent imageContent, bool isGenerated = false)
    {
        // Generate a unique ID for the image if it doesn't have one
        string? imageId = null;
        if (imageContent.AdditionalProperties?.TryGetValue(ImageKey, out imageId) is false || imageId is null)
        {
            imageId = imageContent.Name ?? Guid.NewGuid().ToString();
        }

        if (isGenerated)
        {
            imageContent.AdditionalProperties ??= new();
            imageContent.AdditionalProperties[ImageKey] = imageId;
        }

        // Store the image content for later retrieval
        _imageContentById[imageId] = imageContent;

        return imageId;
    }

    /// <summary>
    /// Replaces FunctionResultContent instances for image generation functions with actual generated image content.
    /// We will have two messages
    /// 1. Role: Assistant, FunctionCall
    /// 2. Role: Tool, FunctionResult
    /// We need to replace content from both but we shouldn't remove the messages.
    /// If we do not then ChatClient's may not accept our altered history.
    /// 
    /// When interating with a HostedImageGenerationTool we will have typically only see a single Message with
    /// Role: Assistant that contains the DataContent (or a provider specific content, that's exposed as DataContent).    
    /// </summary>
    /// <param name="contents">The list of AI content to process.</param>
    private IList<AIContent> ReplaceImageGenerationFunctionResults(IList<AIContent> contents)
    {
        List<AIContent>? newContents = null;

        // Replace FunctionResultContent instances with generated image content
        for (int i = contents.Count - 1; i >= 0; i--)
        {
            var content = contents[i];

            if (content is FunctionCallContent functionCall &&
                _functionNames.Contains(functionCall.Name))
            {
                // create a new list and omit the FunctionCallContent
                newContents ??= CopyList(contents, i);

                // add a placeholder text content to avoid empty contents which could cause the client to drop the message
                newContents.Add(new TextContent(string.Empty));
            }
            else if (content is FunctionResultContent functionResult &&
                _imageContentByCallId.TryGetValue(functionResult.CallId, out var imageContents))
            {
                newContents ??= CopyList(contents, i, imageContents.Count - 1);

                // Insert generated image content in its place, do not preserve the FunctionResultContent
                foreach (var imageContent in imageContents)
                {
                    newContents.Add(imageContent);
                }

                // Remove the mapping as it's no longer needed
                _ = _imageContentByCallId.Remove(functionResult.CallId);
            }
            else
            {
                // keep the existing content if we have a new list
                newContents?.Add(content);
            }
        }

        return newContents ?? contents;
    }

    [Description("Generates images based on a text description.")]
    private async Task<string> GenerateImageAsync(
         [Description("A detailed description of the image to generate")] string prompt,
         CancellationToken cancellationToken = default)
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

        var response = await _imageGenerator.GenerateAsync(request, options, cancellationToken);

        if (response.Contents.Count == 0)
        {
            return "No image was generated.";
        }

        List<string> imageIds = [];
        List<AIContent> imageContents = _imageContentByCallId[callId] = [];
        foreach (var content in response.Contents)
        {
            if (content is DataContent imageContent && imageContent.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                imageContents.Add(imageContent);
                imageIds.Add(StoreImage(imageContent, true));
            }
        }

        return "Generated image successfully.";
    }

    [Description("Lists the identifiers of all images available for edit.")]
    private string[] GetImagesForEdit()
    {
        // Get the call ID from the current function invocation context
        var callId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
        if (callId == null)
        {
            return ["No call ID available for image editing."];
        }

        _imageContentByCallId[callId] = [];

        return _imageContentById.Keys.ToArray();
    }

    [Description("Edits an existing image based on a text description.")]
    private async Task<string> EditImageAsync(
        [Description("A detailed description of the image to generate")] string prompt,
        [Description($"The image to edit from one of the available image identifiers returned by {nameof(GetImagesForEdit)}")] string imageId,
        CancellationToken cancellationToken = default)
    {
        // Get the call ID from the current function invocation context
        var callId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
        if (callId == null)
        {
            return "No call ID available for image editing.";
        }

        if (string.IsNullOrEmpty(imageId))
        {
            return "No imageId provided";
        }

        try
        {
            var originalImage = RetrieveImageContent(imageId);
            if (originalImage == null)
            {
                return $"No image found with: {imageId}";
            }

            var request = new ImageGenerationRequest(prompt, [originalImage]);
            var response = await _imageGenerator.GenerateAsync(request, _imageGenerationOptions, cancellationToken);

            if (response.Contents.Count == 0)
            {
                return "No edited image was generated.";
            }

            List<string> imageIds = [];
            List<AIContent> imageContents = _imageContentByCallId[callId] = [];
            foreach (var content in response.Contents)
            {
                if (content is DataContent imageContent && imageContent.MediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    imageContents.Add(imageContent);
                    imageIds.Add(StoreImage(imageContent, true));
                }
            }

            return "Edited image successfully.";
        }
        catch (FormatException)
        {
            return "Invalid image data format. Please provide a valid base64-encoded image.";
        }
    }
}

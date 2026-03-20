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
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A delegating chat client that enables video generation capabilities by converting <see cref="HostedVideoGenerationTool"/> instances to function tools.</summary>
/// <remarks>
/// <para>
/// The provided implementation of <see cref="IChatClient"/> is thread-safe for concurrent use so long as the
/// <see cref="IVideoGenerator"/> employed is also thread-safe for concurrent use.
/// </para>
/// <para>
/// This client automatically detects <see cref="HostedVideoGenerationTool"/> instances in the <see cref="ChatOptions.Tools"/> collection
/// and replaces them with equivalent function tools that the chat client can invoke to perform video generation and editing operations.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class VideoGeneratingChatClient : DelegatingChatClient
{
    /// <summary>
    /// Specifies how video and other data content is handled when passing data to an inner client.
    /// </summary>
    /// <remarks>
    /// Use this enumeration to control whether videos in the data content are passed as-is, replaced
    /// with unique identifiers, or only generated videos are replaced. This setting affects how downstream clients
    /// receive and process video data.
    /// Reducing what's passed downstream can help manage the context window.
    /// </remarks>
    public enum DataContentHandling
    {
        /// <summary>Pass all DataContent to inner client.</summary>
        None,

        /// <summary>Replace all videos with unique identifiers when passing to inner client.</summary>
        AllVideos,

        /// <summary>Replace only videos that were produced by past video generation requests with unique identifiers when passing to inner client.</summary>
        GeneratedVideos
    }

    private const string VideoKey = "meai_video";

    private readonly IVideoGenerator _videoGenerator;
    private readonly DataContentHandling _dataContentHandling;

    /// <summary>Initializes a new instance of the <see cref="VideoGeneratingChatClient"/> class.</summary>
    /// <param name="innerClient">The underlying <see cref="IChatClient"/>.</param>
    /// <param name="videoGenerator">An <see cref="IVideoGenerator"/> instance that will be used for video generation operations.</param>
    /// <param name="dataContentHandling">Specifies how to handle <see cref="DataContent"/> instances when passing messages to the inner client.
    /// The default is <see cref="DataContentHandling.AllVideos"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> or <paramref name="videoGenerator"/> is <see langword="null"/>.</exception>
    public VideoGeneratingChatClient(IChatClient innerClient, IVideoGenerator videoGenerator, DataContentHandling dataContentHandling = DataContentHandling.AllVideos)
        : base(innerClient)
    {
        _videoGenerator = Throw.IfNull(videoGenerator);
        _dataContentHandling = dataContentHandling;
    }

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        var requestState = new RequestState(_videoGenerator, _dataContentHandling);

        // Process the chat options to replace HostedVideoGenerationTool with functions
        var processedOptions = requestState.ProcessChatOptions(options);
        var processedMessages = requestState.ProcessChatMessages(messages);

        // Get response from base implementation
        var response = await base.GetResponseAsync(processedMessages, processedOptions, cancellationToken);

        // Replace FunctionResultContent instances with generated video content
        foreach (var message in response.Messages)
        {
            message.Contents = requestState.ReplaceVideoGenerationFunctionResults(message.Contents);
        }

        return response;
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        var requestState = new RequestState(_videoGenerator, _dataContentHandling);

        // Process the chat options to replace HostedVideoGenerationTool with functions
        var processedOptions = requestState.ProcessChatOptions(options);
        var processedMessages = requestState.ProcessChatMessages(messages);

        await foreach (var update in base.GetStreamingResponseAsync(processedMessages, processedOptions, cancellationToken))
        {
            // Replace any FunctionResultContent instances with generated video content
            var newContents = requestState.ReplaceVideoGenerationFunctionResults(update.Contents);

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

    /// <summary>Provides a mechanism for releasing unmanaged resources.</summary>
    /// <param name="disposing"><see langword="true"/> to dispose managed resources; otherwise, <see langword="false"/>.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _videoGenerator.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Contains all the per-request state and methods for handling video generation requests.
    /// This class is created fresh for each request to ensure thread safety.
    /// This class is not exposed publicly and does not own any of it's resources.
    /// </summary>
    private sealed class RequestState
    {
        private readonly IVideoGenerator _videoGenerator;
        private readonly DataContentHandling _dataContentHandling;
        private readonly HashSet<string> _toolNames = new(StringComparer.Ordinal);
        private readonly Dictionary<string, List<AIContent>> _videoContentByCallId = [];
        private readonly Dictionary<string, AIContent> _videoContentById = new(StringComparer.OrdinalIgnoreCase);
        private VideoGenerationOptions? _videoGenerationOptions;

        public RequestState(IVideoGenerator videoGenerator, DataContentHandling dataContentHandling)
        {
            _videoGenerator = videoGenerator;
            _dataContentHandling = dataContentHandling;
        }

        /// <summary>
        /// Processes the chat messages to replace videos in data content with unique identifiers as needed.
        /// All videos will be stored for later retrieval during video editing operations.
        /// See <see cref="DataContentHandling"/> for details on video replacement behavior.
        /// </summary>
        /// <param name="messages">Messages to process.</param>
        /// <returns>Processed messages, or the original messages if no changes were made.</returns>
        public IEnumerable<ChatMessage> ProcessChatMessages(IEnumerable<ChatMessage> messages)
        {
            List<ChatMessage>? newMessages = null;
            int messageIndex = 0;
            foreach (var message in messages)
            {
                List<AIContent>? newContents = null;
                for (int contentIndex = 0; contentIndex < message.Contents.Count; contentIndex++)
                {
                    var content = message.Contents[contentIndex];

                    void ReplaceVideo(string videoId, DataContent dataContent)
                    {
                        // Replace video with a placeholder text content, to give an indication to the model of its placement in the context
                        newContents ??= CopyList(message.Contents, contentIndex);
                        newContents.Add(new TextContent($"[{VideoKey}:{videoId}] available for edit.")
                        {
                            Annotations = dataContent.Annotations,
                            AdditionalProperties = dataContent.AdditionalProperties
                        });
                    }

                    if (content is DataContent dataContent && dataContent.HasTopLevelMediaType("video"))
                    {
                        // Store the video to make available for edit
                        var videoId = StoreVideo(dataContent);

                        if (_dataContentHandling == DataContentHandling.AllVideos)
                        {
                            ReplaceVideo(videoId, dataContent);
                            continue; // Skip adding the original content
                        }
                    }
                    else if (content is VideoGenerationToolResultContent toolResultContent)
                    {
                        foreach (var output in toolResultContent.Outputs ?? [])
                        {
                            if (output is DataContent generatedDataContent && generatedDataContent.HasTopLevelMediaType("video"))
                            {
                                // Store the video to make available for edit
                                var videoId = StoreVideo(generatedDataContent, isGenerated: true);

                                if (_dataContentHandling == DataContentHandling.AllVideos ||
                                    _dataContentHandling == DataContentHandling.GeneratedVideos)
                                {
                                    ReplaceVideo(videoId, generatedDataContent);
                                }
                            }
                        }

                        if (_dataContentHandling == DataContentHandling.AllVideos ||
                            _dataContentHandling == DataContentHandling.GeneratedVideos)
                        {
                            // skip adding the generated content
                            continue;
                        }
                    }

                    // Add the original content if no replacement was made
                    newContents?.Add(content);
                }

                if (newContents != null)
                {
                    newMessages ??= [.. messages.Take(messageIndex)];
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

        public ChatOptions? ProcessChatOptions(ChatOptions? options)
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

                // remove all instances of HostedVideoGenerationTool and store the options from the last one
                if (tool is HostedVideoGenerationTool videoGenerationTool)
                {
                    _videoGenerationOptions = videoGenerationTool.Options;

                    // for the first video generation tool, clone the options and insert our function tools
                    // remove any subsequent video generation tools
                    newTools ??= InitializeTools(tools, i);
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

            List<AITool> InitializeTools(IList<AITool> existingTools, int toOffsetExclusive)
            {
#if NET
                ReadOnlySpan<AITool> tools =
#else
                AITool[] tools =
#endif
                [
                    AIFunctionFactory.Create(GenerateVideoAsync),
                    AIFunctionFactory.Create(EditVideoAsync),
                    AIFunctionFactory.Create(GetVideosForEdit)
                ];

                foreach (var tool in tools)
                {
                    _toolNames.Add(tool.Name);
                }

                var result = CopyList(existingTools, toOffsetExclusive, tools.Length);
                result.AddRange(tools);
                return result;
            }
        }

        /// <summary>
        /// Replaces FunctionResultContent instances for video generation functions with actual generated video content.
        /// </summary>
        /// <param name="contents">The list of AI content to process.</param>
        public IList<AIContent> ReplaceVideoGenerationFunctionResults(IList<AIContent> contents)
        {
            List<AIContent>? newContents = null;

            // Replace FunctionResultContent instances with generated video content
            for (int i = contents.Count - 1; i >= 0; i--)
            {
                var content = contents[i];

                // We must lookup by name because in the streaming case we have not yet been called to record the CallId.
                if (content is FunctionCallContent functionCall &&
                    _toolNames.Contains(functionCall.Name))
                {
                    // create a new list and omit the FunctionCallContent
                    newContents ??= CopyList(contents, i);

                    if (functionCall.Name != nameof(GetVideosForEdit))
                    {
                        newContents.Add(new VideoGenerationToolCallContent(functionCall.CallId));
                    }
                }
                else if (content is FunctionResultContent functionResult &&
                    _videoContentByCallId.TryGetValue(functionResult.CallId, out var videoContents))
                {
                    newContents ??= CopyList(contents, i);

                    if (videoContents.Any())
                    {
                        // Insert VideoGenerationToolResultContent in its place, do not preserve the FunctionResultContent
                        newContents.Add(new VideoGenerationToolResultContent(functionResult.CallId)
                        {
                            Outputs = videoContents
                        });
                    }

                    // Remove the mapping as it's no longer needed
                    _ = _videoContentByCallId.Remove(functionResult.CallId);
                }
                else
                {
                    // keep the existing content if we have a new list
                    newContents?.Add(content);
                }
            }

            return newContents ?? contents;
        }

        [Description("Generates videos based on a text description.")]
        public async Task<string> GenerateVideoAsync(
             [Description("A detailed description of the video to generate")] string prompt,
             CancellationToken cancellationToken = default)
        {
            // Get the call ID from the current function invocation context
            var callId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
            if (callId == null)
            {
                return "No call ID available for video generation.";
            }

            var request = new VideoGenerationRequest(prompt);
            var options = _videoGenerationOptions ?? new VideoGenerationOptions();
            options.Count ??= 1;

            var response = await _videoGenerator.GenerateAsync(request, options, cancellationToken: cancellationToken);

            if (response.Contents.Count == 0)
            {
                return "No video was generated.";
            }

            List<string> videoIds = [];
            List<AIContent> videoContents = _videoContentByCallId[callId] = [];
            foreach (var content in response.Contents)
            {
                if (content is DataContent videoContent && videoContent.MediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                {
                    videoContents.Add(videoContent);
                    videoIds.Add(StoreVideo(videoContent, true));
                }
            }

            return "Generated video successfully.";
        }

        [Description("Lists the identifiers of all videos available for edit.")]
        public IEnumerable<string> GetVideosForEdit()
        {
            // Get the call ID from the current function invocation context
            var callId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
            if (callId == null)
            {
                return ["No call ID available for video editing."];
            }

            _videoContentByCallId[callId] = [];

            return _videoContentById.Keys.AsEnumerable();
        }

        [Description("Edits an existing video based on a text description.")]
        public async Task<string> EditVideoAsync(
            [Description("A detailed description of the video to generate")] string prompt,
            [Description($"The video to edit from one of the available video identifiers returned by {nameof(GetVideosForEdit)}")] string videoId,
            CancellationToken cancellationToken = default)
        {
            // Get the call ID from the current function invocation context
            var callId = FunctionInvokingChatClient.CurrentContext?.CallContent.CallId;
            if (callId == null)
            {
                return "No call ID available for video editing.";
            }

            if (string.IsNullOrEmpty(videoId))
            {
                return "No videoId provided";
            }

            try
            {
                var originalVideo = RetrieveVideoContent(videoId);
                if (originalVideo == null)
                {
                    return $"No video found with: {videoId}";
                }

                var request = new VideoGenerationRequest(prompt, [originalVideo]);
                var response = await _videoGenerator.GenerateAsync(request, _videoGenerationOptions, cancellationToken: cancellationToken);

                if (response.Contents.Count == 0)
                {
                    return "No edited video was generated.";
                }

                List<string> videoIds = [];
                List<AIContent> videoContents = _videoContentByCallId[callId] = [];
                foreach (var content in response.Contents)
                {
                    if (content is DataContent videoContent && videoContent.MediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                    {
                        videoContents.Add(videoContent);
                        videoIds.Add(StoreVideo(videoContent, true));
                    }
                }

                return "Edited video successfully.";
            }
            catch (FormatException)
            {
                return "Invalid video data format. Please provide a valid base64-encoded video.";
            }
        }

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

        private DataContent? RetrieveVideoContent(string videoId)
        {
            if (_videoContentById.TryGetValue(videoId, out var videoContent))
            {
                return videoContent as DataContent;
            }

            return null;
        }

        private string StoreVideo(DataContent videoContent, bool isGenerated = false)
        {
            // Generate a unique ID for the video if it doesn't have one
            string? videoId = null;
            if (videoContent.AdditionalProperties?.TryGetValue(VideoKey, out videoId) is false || videoId is null)
            {
                videoId = videoContent.Name ?? Guid.NewGuid().ToString();
            }

            if (isGenerated)
            {
                videoContent.AdditionalProperties ??= [];
                videoContent.AdditionalProperties[VideoKey] = videoId;
            }

            // Store the video content for later retrieval
            _videoContentById[videoId] = videoContent;

            return videoId;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Chat;

#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable EA0011 // Consider removing unnecessary conditional access operator (?)
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="ChatClient"/>.</summary>
internal sealed class OpenAIChatClient : IChatClient
{
    /// <summary>Metadata about the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The underlying <see cref="ChatClient" />.</summary>
    private readonly ChatClient _chatClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAIChatClient"/> class for the specified <see cref="ChatClient"/>.</summary>
    /// <param name="chatClient">The underlying client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="chatClient"/> is <see langword="null"/>.</exception>
    public OpenAIChatClient(ChatClient chatClient)
    {
        _ = Throw.IfNull(chatClient);

        _chatClient = chatClient;

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(ChatClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(chatClient) as Uri ?? OpenAIClientExtensions.DefaultOpenAIEndpoint;

        _metadata = new("openai", providerUrl, _chatClient.Model);
    }

    /// <inheritdoc />
    object? IChatClient.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(ChatClientMetadata) ? _metadata :
            serviceType == typeof(ChatClient) ? _chatClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        var openAIChatMessages = ToOpenAIChatMessages(messages, options);
        var openAIOptions = ToOpenAIOptions(options);

        // Make the call to OpenAI.
        var response = await _chatClient.CompleteChatAsync(openAIChatMessages, openAIOptions, cancellationToken).ConfigureAwait(false);

        return FromOpenAIChatCompletion(response.Value, openAIOptions);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        var openAIChatMessages = ToOpenAIChatMessages(messages, options);
        var openAIOptions = ToOpenAIOptions(options);

        // Make the call to OpenAI.
        var chatCompletionUpdates = _chatClient.CompleteChatStreamingAsync(openAIChatMessages, openAIOptions, cancellationToken);

        return FromOpenAIStreamingChatCompletionAsync(chatCompletionUpdates, openAIOptions, cancellationToken);
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IChatClient interface.
    }

    /// <summary>Converts an Extensions function to an OpenAI chat tool.</summary>
    internal static ChatTool ToOpenAIChatTool(AIFunction aiFunction, ChatOptions? options = null)
    {
        bool? strict =
            OpenAIClientExtensions.HasStrict(aiFunction.AdditionalProperties) ??
            OpenAIClientExtensions.HasStrict(options?.AdditionalProperties);

        return ChatTool.CreateFunctionTool(
            aiFunction.Name,
            aiFunction.Description,
            OpenAIClientExtensions.ToOpenAIFunctionParameters(aiFunction, strict),
            strict);
    }

    /// <summary>Converts an Extensions chat message enumerable to an OpenAI chat message enumerable.</summary>
    internal static IEnumerable<OpenAI.Chat.ChatMessage> ToOpenAIChatMessages(IEnumerable<ChatMessage> inputs, ChatOptions? chatOptions)
    {
        // Maps all of the M.E.AI types to the corresponding OpenAI types.
        // Unrecognized or non-processable content is ignored.

        if (chatOptions?.Instructions is { } instructions && !string.IsNullOrWhiteSpace(instructions))
        {
            yield return new SystemChatMessage(instructions);
        }

        foreach (ChatMessage input in inputs)
        {
            if (input.RawRepresentation is OpenAI.Chat.ChatMessage raw)
            {
                yield return raw;
                continue;
            }

            if (input.Role == ChatRole.System ||
                input.Role == ChatRole.User ||
                input.Role == OpenAIClientExtensions.ChatRoleDeveloper)
            {
                var parts = ToOpenAIChatContent(input.Contents);
                yield return
                    input.Role == ChatRole.System ? new SystemChatMessage(parts) { ParticipantName = input.AuthorName } :
                    input.Role == OpenAIClientExtensions.ChatRoleDeveloper ? new DeveloperChatMessage(parts) { ParticipantName = input.AuthorName } :
                    new UserChatMessage(parts) { ParticipantName = input.AuthorName };
            }
            else if (input.Role == ChatRole.Tool)
            {
                foreach (AIContent item in input.Contents)
                {
                    if (item is FunctionResultContent resultContent)
                    {
                        string? result = resultContent.Result as string;
                        if (result is null && resultContent.Result is not null)
                        {
                            try
                            {
                                result = JsonSerializer.Serialize(resultContent.Result, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(object)));
                            }
                            catch (NotSupportedException)
                            {
                                // If the type can't be serialized, skip it.
                            }
                        }

                        yield return new ToolChatMessage(resultContent.CallId, result ?? string.Empty);
                    }
                }
            }
            else if (input.Role == ChatRole.Assistant)
            {
                List<ChatMessageContentPart>? contentParts = null;
                List<ChatToolCall>? toolCalls = null;
                string? refusal = null;
                foreach (var content in input.Contents)
                {
                    switch (content)
                    {
                        case ErrorContent ec when ec.ErrorCode == nameof(AssistantChatMessage.Refusal):
                            refusal = ec.Message;
                            break;

                        case FunctionCallContent fc:
                            (toolCalls ??= []).Add(
                                ChatToolCall.CreateFunctionToolCall(fc.CallId, fc.Name, new(JsonSerializer.SerializeToUtf8Bytes(
                                    fc.Arguments, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(IDictionary<string, object?>))))));
                            break;

                        default:
                            if (ToChatMessageContentPart(content) is { } part)
                            {
                                (contentParts ??= []).Add(part);
                            }

                            break;
                    }
                }

                AssistantChatMessage message;
                if (contentParts is not null)
                {
                    message = new(contentParts);
                    if (toolCalls is not null)
                    {
                        foreach (var toolCall in toolCalls)
                        {
                            message.ToolCalls.Add(toolCall);
                        }
                    }
                }
                else
                {
                    message = toolCalls is not null ?
                        new(toolCalls) :
                        new(ChatMessageContentPart.CreateTextPart(string.Empty));
                }

                message.ParticipantName = input.AuthorName;
                message.Refusal = refusal;

                yield return message;
            }
        }
    }

    /// <summary>Converts a list of <see cref="AIContent"/> to a list of <see cref="ChatMessageContentPart"/>.</summary>
    internal static List<ChatMessageContentPart> ToOpenAIChatContent(IEnumerable<AIContent> contents)
    {
        List<ChatMessageContentPart> parts = [];

        foreach (var content in contents)
        {
            if (content.RawRepresentation is ChatMessageContentPart raw)
            {
                parts.Add(raw);
            }
            else
            {
                if (ToChatMessageContentPart(content) is { } part)
                {
                    parts.Add(part);
                }
            }
        }

        if (parts.Count == 0)
        {
            parts.Add(ChatMessageContentPart.CreateTextPart(string.Empty));
        }

        return parts;
    }

    private static ChatMessageContentPart? ToChatMessageContentPart(AIContent content)
    {
        switch (content)
        {
            case AIContent when content.RawRepresentation is ChatMessageContentPart rawContentPart:
                return rawContentPart;

            case TextContent textContent:
                return ChatMessageContentPart.CreateTextPart(textContent.Text);

            case UriContent uriContent when uriContent.HasTopLevelMediaType("image"):
                return ChatMessageContentPart.CreateImagePart(uriContent.Uri, GetImageDetail(content));

            case DataContent dataContent when dataContent.HasTopLevelMediaType("image"):
                return ChatMessageContentPart.CreateImagePart(BinaryData.FromBytes(dataContent.Data), dataContent.MediaType, GetImageDetail(content));

            case DataContent dataContent when dataContent.HasTopLevelMediaType("audio"):
                var audioData = BinaryData.FromBytes(dataContent.Data);
                if (dataContent.MediaType.Equals("audio/mpeg", StringComparison.OrdinalIgnoreCase))
                {
                    return ChatMessageContentPart.CreateInputAudioPart(audioData, ChatInputAudioFormat.Mp3);
                }
                else if (dataContent.MediaType.Equals("audio/wav", StringComparison.OrdinalIgnoreCase))
                {
                    return ChatMessageContentPart.CreateInputAudioPart(audioData, ChatInputAudioFormat.Wav);
                }

                break;

            case DataContent dataContent when dataContent.MediaType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase):
                return ChatMessageContentPart.CreateFilePart(BinaryData.FromBytes(dataContent.Data), dataContent.MediaType, dataContent.Name ?? $"{Guid.NewGuid():N}.pdf");

            case HostedFileContent fileContent:
                return ChatMessageContentPart.CreateFilePart(fileContent.FileId);
        }

        return null;
    }

    private static ChatImageDetailLevel? GetImageDetail(AIContent content)
    {
        if (content.AdditionalProperties?.TryGetValue("detail", out object? value) is true)
        {
            return value switch
            {
                string detailString => new ChatImageDetailLevel(detailString),
                ChatImageDetailLevel detail => detail,
                _ => null
            };
        }

        return null;
    }

    internal static async IAsyncEnumerable<ChatResponseUpdate> FromOpenAIStreamingChatCompletionAsync(
        IAsyncEnumerable<StreamingChatCompletionUpdate> updates,
        ChatCompletionOptions? options,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Dictionary<int, FunctionCallInfo>? functionCallInfos = null;
        ChatRole? streamedRole = null;
        ChatFinishReason? finishReason = null;
        StringBuilder? refusal = null;
        string? responseId = null;
        DateTimeOffset? createdAt = null;
        string? modelId = null;

        // Process each update as it arrives
        await foreach (StreamingChatCompletionUpdate update in updates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // The role and finish reason may arrive during any update, but once they've arrived, the same value should be the same for all subsequent updates.
            streamedRole ??= update.Role is ChatMessageRole role ? FromOpenAIChatRole(role) : null;
            finishReason ??= update.FinishReason is OpenAI.Chat.ChatFinishReason reason ? FromOpenAIFinishReason(reason) : null;
            responseId ??= update.CompletionId;
            createdAt ??= update.CreatedAt;
            modelId ??= update.Model;

            // Create the response content object.
            ChatResponseUpdate responseUpdate = new()
            {
                ResponseId = update.CompletionId,
                MessageId = update.CompletionId, // There is no per-message ID, but there's only one message per response, so use the response ID
                CreatedAt = update.CreatedAt,
                FinishReason = finishReason,
                ModelId = modelId,
                RawRepresentation = update,
                Role = streamedRole,
            };

            // Transfer over content update items.
            if (update.ContentUpdate is { Count: > 0 })
            {
                ConvertContentParts(update.ContentUpdate, responseUpdate.Contents);
            }

            if (update.OutputAudioUpdate is { } audioUpdate)
            {
                responseUpdate.Contents.Add(new DataContent(audioUpdate.AudioBytesUpdate.ToMemory(), GetOutputAudioMimeType(options))
                {
                    RawRepresentation = audioUpdate,
                });
            }

            // Transfer over refusal updates.
            if (update.RefusalUpdate is not null)
            {
                _ = (refusal ??= new()).Append(update.RefusalUpdate);
            }

            // Transfer over tool call updates.
            if (update.ToolCallUpdates is { Count: > 0 } toolCallUpdates)
            {
                foreach (StreamingChatToolCallUpdate toolCallUpdate in toolCallUpdates)
                {
                    functionCallInfos ??= [];
                    if (!functionCallInfos.TryGetValue(toolCallUpdate.Index, out FunctionCallInfo? existing))
                    {
                        functionCallInfos[toolCallUpdate.Index] = existing = new();
                    }

                    existing.CallId ??= toolCallUpdate.ToolCallId;
                    existing.Name ??= toolCallUpdate.FunctionName;
                    if (toolCallUpdate.FunctionArgumentsUpdate is { } argUpdate && !argUpdate.ToMemory().IsEmpty)
                    {
                        _ = (existing.Arguments ??= new()).Append(argUpdate.ToString());
                    }
                }
            }

            // Transfer over usage updates.
            if (update.Usage is ChatTokenUsage tokenUsage)
            {
                responseUpdate.Contents.Add(new UsageContent(FromOpenAIUsage(tokenUsage))
                {
                    RawRepresentation = tokenUsage,
                });
            }

            // Now yield the item.
            yield return responseUpdate;
        }

        // Now that we've received all updates, combine any for function calls into a single item to yield.
        if (functionCallInfos is not null)
        {
            ChatResponseUpdate responseUpdate = new()
            {
                ResponseId = responseId,
                MessageId = responseId, // There is no per-message ID, but there's only one message per response, so use the response ID
                CreatedAt = createdAt,
                FinishReason = finishReason,
                ModelId = modelId,
                Role = streamedRole,
            };

            foreach (var entry in functionCallInfos)
            {
                FunctionCallInfo fci = entry.Value;
                if (!string.IsNullOrWhiteSpace(fci.Name))
                {
                    var callContent = OpenAIClientExtensions.ParseCallContent(
                        fci.Arguments?.ToString() ?? string.Empty,
                        fci.CallId!,
                        fci.Name!);
                    responseUpdate.Contents.Add(callContent);
                }
            }

            // Refusals are about the model not following the schema for tool calls. As such, if we have any refusal,
            // add it to this function calling item.
            if (refusal is not null)
            {
                responseUpdate.Contents.Add(new ErrorContent(refusal.ToString()) { ErrorCode = "Refusal" });
            }

            yield return responseUpdate;
        }
    }

    private static string GetOutputAudioMimeType(ChatCompletionOptions? options) =>
        options?.AudioOptions?.OutputAudioFormat.ToString()?.ToLowerInvariant() switch
        {
            "opus" => "audio/opus",
            "aac" => "audio/aac",
            "flac" => "audio/flac",
            "wav" => "audio/wav",
            "pcm" => "audio/pcm",
            "mp3" or _ => "audio/mpeg",
        };

    internal static ChatResponse FromOpenAIChatCompletion(ChatCompletion openAICompletion, ChatCompletionOptions? chatCompletionOptions)
    {
        _ = Throw.IfNull(openAICompletion);

        // Create the return message.
        ChatMessage returnMessage = new()
        {
            CreatedAt = openAICompletion.CreatedAt,
            MessageId = openAICompletion.Id, // There's no per-message ID, so we use the same value as the response ID
            RawRepresentation = openAICompletion,
            Role = FromOpenAIChatRole(openAICompletion.Role),
        };

        // Populate its content from those in the OpenAI response content.
        foreach (ChatMessageContentPart contentPart in openAICompletion.Content)
        {
            if (ToAIContent(contentPart) is AIContent aiContent)
            {
                returnMessage.Contents.Add(aiContent);
            }
        }

        // Output audio is handled separately from message content parts.
        if (openAICompletion.OutputAudio is ChatOutputAudio audio)
        {
            returnMessage.Contents.Add(new DataContent(audio.AudioBytes.ToMemory(), GetOutputAudioMimeType(chatCompletionOptions))
            {
                RawRepresentation = audio,
            });
        }

        // Also manufacture function calling content items from any tool calls in the response.
        foreach (ChatToolCall toolCall in openAICompletion.ToolCalls)
        {
            if (!string.IsNullOrWhiteSpace(toolCall.FunctionName))
            {
                var callContent = OpenAIClientExtensions.ParseCallContent(toolCall.FunctionArguments, toolCall.Id, toolCall.FunctionName);
                callContent.RawRepresentation = toolCall;

                returnMessage.Contents.Add(callContent);
            }
        }

        // And add error content for any refusals, which represent errors in generating output that conforms to a provided schema.
        if (openAICompletion.Refusal is string refusal)
        {
            returnMessage.Contents.Add(new ErrorContent(refusal) { ErrorCode = nameof(openAICompletion.Refusal) });
        }

        // And add annotations. OpenAI chat completion specifies annotations at the message level (and as such they can't be
        // roundtripped back); we store them either on the first text content, assuming there is one, or on a dedicated content
        // instance if not.
        if (openAICompletion.Annotations is { Count: > 0 })
        {
            TextContent? annotationContent = returnMessage.Contents.OfType<TextContent>().FirstOrDefault();
            if (annotationContent is null)
            {
                annotationContent = new(null);
                returnMessage.Contents.Add(annotationContent);
            }

            foreach (var annotation in openAICompletion.Annotations)
            {
                (annotationContent.Annotations ??= []).Add(new CitationAnnotation
                {
                    RawRepresentation = annotation,
                    AnnotatedRegions = [new TextSpanAnnotatedRegion { StartIndex = annotation.StartIndex, EndIndex = annotation.EndIndex }],
                    Title = annotation.WebResourceTitle,
                    Url = annotation.WebResourceUri,
                });
            }
        }

        // Wrap the content in a ChatResponse to return.
        var response = new ChatResponse(returnMessage)
        {
            CreatedAt = openAICompletion.CreatedAt,
            FinishReason = FromOpenAIFinishReason(openAICompletion.FinishReason),
            ModelId = openAICompletion.Model,
            RawRepresentation = openAICompletion,
            ResponseId = openAICompletion.Id,
        };

        if (openAICompletion.Usage is ChatTokenUsage tokenUsage)
        {
            response.Usage = FromOpenAIUsage(tokenUsage);
        }

        return response;
    }

    /// <summary>Converts an extensions options instance to an OpenAI options instance.</summary>
    private ChatCompletionOptions ToOpenAIOptions(ChatOptions? options)
    {
        if (options is null)
        {
            return new ChatCompletionOptions();
        }

        if (options.RawRepresentationFactory?.Invoke(this) is not ChatCompletionOptions result)
        {
            result = new ChatCompletionOptions();
        }

        result.FrequencyPenalty ??= options.FrequencyPenalty;
        result.MaxOutputTokenCount ??= options.MaxOutputTokens;
        result.TopP ??= options.TopP;
        result.PresencePenalty ??= options.PresencePenalty;
        result.Temperature ??= options.Temperature;
        result.AllowParallelToolCalls ??= options.AllowMultipleToolCalls;
        result.Seed ??= options.Seed;

        if (options.StopSequences is { Count: > 0 } stopSequences)
        {
            foreach (string stopSequence in stopSequences)
            {
                result.StopSequences.Add(stopSequence);
            }
        }

        if (options.Tools is { Count: > 0 } tools)
        {
            foreach (AITool tool in tools)
            {
                if (tool is AIFunction af)
                {
                    result.Tools.Add(ToOpenAIChatTool(af, options));
                }
            }

            if (result.ToolChoice is null && result.Tools.Count > 0)
            {
                switch (options.ToolMode)
                {
                    case NoneChatToolMode:
                        result.ToolChoice = ChatToolChoice.CreateNoneChoice();
                        break;

                    case AutoChatToolMode:
                    case null:
                        result.ToolChoice = ChatToolChoice.CreateAutoChoice();
                        break;

                    case RequiredChatToolMode required:
                        result.ToolChoice = required.RequiredFunctionName is null ?
                            ChatToolChoice.CreateRequiredChoice() :
                            ChatToolChoice.CreateFunctionChoice(required.RequiredFunctionName);
                        break;
                }
            }
        }

        if (result.ResponseFormat is null)
        {
            if (options.ResponseFormat is ChatResponseFormatText)
            {
                result.ResponseFormat = OpenAI.Chat.ChatResponseFormat.CreateTextFormat();
            }
            else if (options.ResponseFormat is ChatResponseFormatJson jsonFormat)
            {
                result.ResponseFormat = OpenAIClientExtensions.StrictSchemaTransformCache.GetOrCreateTransformedSchema(jsonFormat) is { } jsonSchema ?
                    OpenAI.Chat.ChatResponseFormat.CreateJsonSchemaFormat(
                        jsonFormat.SchemaName ?? "json_schema",
                        BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(jsonSchema, OpenAIJsonContext.Default.JsonElement)),
                        jsonFormat.SchemaDescription,
                        OpenAIClientExtensions.HasStrict(options.AdditionalProperties)) :
                    OpenAI.Chat.ChatResponseFormat.CreateJsonObjectFormat();
            }
        }

        return result;
    }

    private static UsageDetails FromOpenAIUsage(ChatTokenUsage tokenUsage)
    {
        var destination = new UsageDetails
        {
            InputTokenCount = tokenUsage.InputTokenCount,
            OutputTokenCount = tokenUsage.OutputTokenCount,
            TotalTokenCount = tokenUsage.TotalTokenCount,
            AdditionalCounts = [],
        };

        var counts = destination.AdditionalCounts;

        if (tokenUsage.InputTokenDetails is ChatInputTokenUsageDetails inputDetails)
        {
            const string InputDetails = nameof(ChatTokenUsage.InputTokenDetails);
            counts.Add($"{InputDetails}.{nameof(ChatInputTokenUsageDetails.AudioTokenCount)}", inputDetails.AudioTokenCount);
            counts.Add($"{InputDetails}.{nameof(ChatInputTokenUsageDetails.CachedTokenCount)}", inputDetails.CachedTokenCount);
        }

        if (tokenUsage.OutputTokenDetails is ChatOutputTokenUsageDetails outputDetails)
        {
            const string OutputDetails = nameof(ChatTokenUsage.OutputTokenDetails);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.ReasoningTokenCount)}", outputDetails.ReasoningTokenCount);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.AudioTokenCount)}", outputDetails.AudioTokenCount);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.AcceptedPredictionTokenCount)}", outputDetails.AcceptedPredictionTokenCount);
            counts.Add($"{OutputDetails}.{nameof(ChatOutputTokenUsageDetails.RejectedPredictionTokenCount)}", outputDetails.RejectedPredictionTokenCount);
        }

        return destination;
    }

    /// <summary>Converts an OpenAI role to an Extensions role.</summary>
    private static ChatRole FromOpenAIChatRole(ChatMessageRole role) =>
        role switch
        {
            ChatMessageRole.System => ChatRole.System,
            ChatMessageRole.User => ChatRole.User,
            ChatMessageRole.Assistant => ChatRole.Assistant,
            ChatMessageRole.Tool => ChatRole.Tool,
            ChatMessageRole.Developer => OpenAIClientExtensions.ChatRoleDeveloper,
            _ => new ChatRole(role.ToString()),
        };

    /// <summary>Creates <see cref="AIContent"/>s from <see cref="ChatMessageContent"/>.</summary>
    /// <param name="content">The content parts to convert into a content.</param>
    /// <param name="results">The result collection into which to write the resulting content.</param>
    internal static void ConvertContentParts(ChatMessageContent content, IList<AIContent> results)
    {
        foreach (ChatMessageContentPart contentPart in content)
        {
            if (ToAIContent(contentPart) is { } aiContent)
            {
                results.Add(aiContent);
            }
        }
    }

    /// <summary>Creates an <see cref="AIContent"/> from a <see cref="ChatMessageContentPart"/>.</summary>
    /// <param name="contentPart">The content part to convert into a content.</param>
    /// <returns>The constructed <see cref="AIContent"/>, or <see langword="null"/> if the content part could not be converted.</returns>
    private static AIContent? ToAIContent(ChatMessageContentPart contentPart)
    {
        AIContent? aiContent = null;

        switch (contentPart.Kind)
        {
            case ChatMessageContentPartKind.Text:
                aiContent = new TextContent(contentPart.Text);
                break;

            case ChatMessageContentPartKind.Image:
                aiContent =
                    contentPart.ImageUri is not null ? new UriContent(contentPart.ImageUri, "image/*") :
                    contentPart.ImageBytes is not null ? new DataContent(contentPart.ImageBytes.ToMemory(), contentPart.ImageBytesMediaType) :
                    null;

                if (aiContent is not null && contentPart.ImageDetailLevel?.ToString() is string detail)
                {
                    (aiContent.AdditionalProperties ??= [])[nameof(contentPart.ImageDetailLevel)] = detail;
                }

                break;

            case ChatMessageContentPartKind.File:
                aiContent =
                    contentPart.FileId is not null ? new HostedFileContent(contentPart.FileId) :
                    contentPart.FileBytes is not null ? new DataContent(contentPart.FileBytes.ToMemory(), contentPart.FileBytesMediaType) { Name = contentPart.Filename } :
                    null;
                break;
        }

        if (aiContent is not null)
        {
            if (contentPart.Refusal is string refusal)
            {
                (aiContent.AdditionalProperties ??= [])[nameof(contentPart.Refusal)] = refusal;
            }

            aiContent.RawRepresentation = contentPart;
        }

        return aiContent;
    }

    /// <summary>Converts an OpenAI finish reason to an Extensions finish reason.</summary>
    private static ChatFinishReason? FromOpenAIFinishReason(OpenAI.Chat.ChatFinishReason? finishReason) =>
        finishReason?.ToString() is not string s ? null :
        finishReason switch
        {
            OpenAI.Chat.ChatFinishReason.Stop => ChatFinishReason.Stop,
            OpenAI.Chat.ChatFinishReason.Length => ChatFinishReason.Length,
            OpenAI.Chat.ChatFinishReason.ContentFilter => ChatFinishReason.ContentFilter,
            OpenAI.Chat.ChatFinishReason.ToolCalls or OpenAI.Chat.ChatFinishReason.FunctionCall => ChatFinishReason.ToolCalls,
            _ => new ChatFinishReason(s),
        };

    /// <summary>POCO representing function calling info. Used to concatenation information for a single function call from across multiple streaming updates.</summary>
    private sealed class FunctionCallInfo
    {
        public string? CallId;
        public string? Name;
        public StringBuilder? Arguments;
    }
}

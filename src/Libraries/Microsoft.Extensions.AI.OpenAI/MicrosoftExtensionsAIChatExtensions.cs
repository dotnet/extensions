// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

#pragma warning disable S103 // Lines should not be too long

namespace OpenAI.Chat;

/// <summary>Provides extension methods for working with content associated with OpenAI.Chat.</summary>
public static class MicrosoftExtensionsAIChatExtensions
{
    /// <summary>Creates an OpenAI <see cref="ChatTool"/> from an <see cref="AIFunction"/>.</summary>
    /// <param name="function">The function to convert.</param>
    /// <returns>An OpenAI <see cref="ChatTool"/> representing <paramref name="function"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="function"/> is <see langword="null"/>.</exception>
    public static ChatTool AsOpenAIChatTool(this AIFunction function) =>
        OpenAIChatClient.ToOpenAIChatTool(Throw.IfNull(function));

    /// <summary>Creates a sequence of OpenAI <see cref="ChatMessage"/> instances from the specified input messages.</summary>
    /// <param name="messages">The input messages to convert.</param>
    /// <returns>A sequence of OpenAI chat messages.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    public static IEnumerable<ChatMessage> AsOpenAIChatMessages(this IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages) =>
        OpenAIChatClient.ToOpenAIChatMessages(Throw.IfNull(messages), chatOptions: null);

    /// <summary>Creates an OpenAI <see cref="ChatCompletion"/> from a <see cref="ChatResponse"/>.</summary>
    /// <param name="response">The <see cref="ChatResponse"/> to convert to a <see cref="ChatCompletion"/>.</param>
    /// <returns>A converted <see cref="ChatCompletion"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    public static ChatCompletion AsOpenAIChatCompletion(this ChatResponse response)
    {
        _ = Throw.IfNull(response);

        if (response.RawRepresentation is ChatCompletion chatCompletion)
        {
            return chatCompletion;
        }

        var lastMessage = response.Messages.LastOrDefault();

        ChatMessageRole role = lastMessage?.Role.Value switch
        {
            "user" => ChatMessageRole.User,
            "function" => ChatMessageRole.Function,
            "tool" => ChatMessageRole.Tool,
            "developer" => ChatMessageRole.Developer,
            "system" => ChatMessageRole.System,
            _ => ChatMessageRole.Assistant,
        };

        ChatFinishReason finishReason = response.FinishReason?.Value switch
        {
            "length" => ChatFinishReason.Length,
            "content_filter" => ChatFinishReason.ContentFilter,
            "tool_calls" => ChatFinishReason.ToolCalls,
            "function_call" => ChatFinishReason.FunctionCall,
            _ => ChatFinishReason.Stop,
        };

        ChatTokenUsage usage = OpenAIChatModelFactory.ChatTokenUsage(
            (int?)response.Usage?.InputTokenCount ?? 0,
            (int?)response.Usage?.OutputTokenCount ?? 0,
            (int?)response.Usage?.TotalTokenCount ?? 0);

        IEnumerable<ChatToolCall>? toolCalls = lastMessage?.Contents
            .OfType<FunctionCallContent>().Select(c => ChatToolCall.CreateFunctionToolCall(c.CallId, c.Name,
                new BinaryData(JsonSerializer.SerializeToUtf8Bytes(c.Arguments, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(IDictionary<string, object?>))))));

        return OpenAIChatModelFactory.ChatCompletion(
            response.ResponseId,
            finishReason,
            new(OpenAIChatClient.ToOpenAIChatContent(lastMessage?.Contents ?? [])),
            toolCalls: toolCalls,
            role: role,
            createdAt: response.CreatedAt ?? default,
            model: response.ModelId,
            usage: usage,
            outputAudio: lastMessage?.Contents.OfType<DataContent>().Where(dc => dc.HasTopLevelMediaType("audio")).Select(a => OpenAIChatModelFactory.ChatOutputAudio(new(a.Data))).FirstOrDefault(),
            messageAnnotations: ConvertAnnotations(lastMessage?.Contents));

        static IEnumerable<ChatMessageAnnotation> ConvertAnnotations(IEnumerable<AIContent>? contents)
        {
            if (contents is null)
            {
                yield break;
            }

            foreach (var content in contents)
            {
                if (content.Annotations is null)
                {
                    continue;
                }

                foreach (var annotation in content.Annotations)
                {
                    if (annotation is not CitationAnnotation citation)
                    {
                        continue;
                    }

                    if (citation.AnnotatedRegions?.OfType<TextSpanAnnotatedRegion>().ToArray() is { Length: > 0 } regions)
                    {
                        foreach (var region in regions)
                        {
                            yield return OpenAIChatModelFactory.ChatMessageAnnotation(region.StartIndex ?? 0, region.EndIndex ?? 0, citation.Url, citation.Title);
                        }
                    }
                    else
                    {
                        yield return OpenAIChatModelFactory.ChatMessageAnnotation(0, 0, citation.Url, citation.Title);
                    }
                }
            }
        }
    }

    /// <summary>Creates a sequence of <see cref="Microsoft.Extensions.AI.ChatMessage"/> instances from the specified input messages.</summary>
    /// <param name="messages">The input messages to convert.</param>
    /// <returns>A sequence of Microsoft.Extensions.AI chat messages.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="messages"/> is <see langword="null"/>.</exception>
    public static IEnumerable<Microsoft.Extensions.AI.ChatMessage> AsChatMessages(this IEnumerable<ChatMessage> messages)
    {
        _ = Throw.IfNull(messages);

        foreach (var message in messages)
        {
            Microsoft.Extensions.AI.ChatMessage resultMessage = new()
            {
                RawRepresentation = message,
            };

            switch (message)
            {
                case AssistantChatMessage acm:
                    resultMessage.AuthorName = acm.ParticipantName;
                    OpenAIChatClient.ConvertContentParts(acm.Content, resultMessage.Contents);
                    foreach (var toolCall in acm.ToolCalls)
                    {
                        var fcc = OpenAIClientExtensions.ParseCallContent(toolCall.FunctionArguments, toolCall.Id, toolCall.FunctionName);
                        fcc.RawRepresentation = toolCall;
                        resultMessage.Contents.Add(fcc);
                    }

                    break;

                case UserChatMessage ucm:
                    resultMessage.AuthorName = ucm.ParticipantName;
                    OpenAIChatClient.ConvertContentParts(ucm.Content, resultMessage.Contents);
                    break;

                case DeveloperChatMessage dcm:
                    resultMessage.AuthorName = dcm.ParticipantName;
                    OpenAIChatClient.ConvertContentParts(dcm.Content, resultMessage.Contents);
                    break;

                case SystemChatMessage scm:
                    resultMessage.AuthorName = scm.ParticipantName;
                    OpenAIChatClient.ConvertContentParts(scm.Content, resultMessage.Contents);
                    break;

                case ToolChatMessage tcm:
                    resultMessage.Contents.Add(new FunctionResultContent(tcm.ToolCallId, ToToolResult(tcm.Content))
                    {
                        RawRepresentation = tcm,
                    });

                    static object ToToolResult(ChatMessageContent content)
                    {
                        if (content.Count == 1 && content[0] is { Text: { } text })
                        {
                            return text;
                        }

                        MemoryStream ms = new();
                        using Utf8JsonWriter writer = new(ms, new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
                        foreach (IJsonModel<ChatMessageContentPart> part in content)
                        {
                            part.Write(writer, ModelReaderWriterOptions.Json);
                        }

                        return JsonSerializer.Deserialize(ms.GetBuffer().AsSpan(0, (int)ms.Position), AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(JsonElement)))!;
                    }

                    break;
            }

            yield return resultMessage;
        }
    }

    /// <summary>Creates a Microsoft.Extensions.AI <see cref="ChatResponse"/> from a <see cref="ChatCompletion"/>.</summary>
    /// <param name="chatCompletion">The <see cref="ChatCompletion"/> to convert to a <see cref="ChatResponse"/>.</param>
    /// <param name="options">The options employed in the creation of the response.</param>
    /// <returns>A converted <see cref="ChatResponse"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="chatCompletion"/> is <see langword="null"/>.</exception>
    public static ChatResponse AsChatResponse(this ChatCompletion chatCompletion, ChatCompletionOptions? options = null) =>
        OpenAIChatClient.FromOpenAIChatCompletion(Throw.IfNull(chatCompletion), options);
}

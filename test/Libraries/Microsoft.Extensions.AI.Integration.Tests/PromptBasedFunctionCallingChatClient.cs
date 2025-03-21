// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S3459 // Unassigned members should be removed

namespace Microsoft.Extensions.AI;

// Demonstrates how custom clients can layer in non-trivial functionality.
// In this case we're able to upgrade non-function-calling models
// to behaving as if they do support function calling.
//
// In practice:
//  - For llama3:8b or mistral:7b, this works fairly reliably, at least when it only needs to
//    make a single function call with a constrained set of args.
//  - For smaller models like phi3:mini, it works only on a more occasional basis (e.g., if there's
//    only one function defined, and it takes no arguments, but is very hit-and-miss beyond that).

internal sealed class PromptBasedFunctionCallingChatClient(IChatClient innerClient)
    : DelegatingChatClient(innerClient)
{
    private const string MessageIntro = "You are an AI model with function calling capabilities. Call one or more functions if they are relevant to the user's query.";

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Our goal is to convert tools into a prompt describing them, then to detect tool calls in the
        // response and convert those into FunctionCallContent.
        if (options?.Tools is { Count: > 0 })
        {
            List<ChatMessage> chatMessagesList = [CreateToolPrompt(options.Tools), .. chatMessages.Select(m => m.Clone())];
            chatMessages = chatMessagesList;
            options = options.Clone();
            options.Tools = null;

            options.StopSequences ??= [];
            if (!options.StopSequences.Contains("</tool_calls>"))
            {
                options.StopSequences.Add("</tool_calls>");
            }

            // Since the point of this client is to avoid relying on the underlying model having
            // native tool call support, we have to replace any "tool" or "toolcall" messages with
            // "user" or "assistant" ones. We don't mutate the incoming messages, because the
            // intent is only to modify the representation we send to the underlying model.
            for (var messageIndex = 0; messageIndex < chatMessagesList.Count; messageIndex++)
            {
                var message = chatMessagesList[messageIndex];
                for (var itemIndex = 0; itemIndex < message.Contents.Count; itemIndex++)
                {
                    if (message.Contents[itemIndex] is FunctionResultContent frc)
                    {
                        var toolCallResultJson = JsonSerializer.Serialize(new ToolCallResult { Id = frc.CallId, Result = frc.Result }, _jsonOptions);
                        chatMessagesList[messageIndex] = new ChatMessage(ChatRole.User, $"<tool_call_result>{toolCallResultJson}</tool_call_result>");
                    }
                    else if (message.Contents[itemIndex] is FunctionCallContent fcc)
                    {
                        var toolCallJson = JsonSerializer.Serialize(new { fcc.CallId, fcc.Name, fcc.Arguments }, _jsonOptions);
                        chatMessagesList[messageIndex] = new ChatMessage(ChatRole.Assistant, $"<tool_call_json>{toolCallJson}</tool_call_json>");
                    }
                }
            }
        }

        var result = await base.GetResponseAsync(chatMessages, options, cancellationToken);

        if (result.Text is { } content
            && content.IndexOf("<tool_call_json>", StringComparison.Ordinal) is int startPos
            && startPos >= 0)
        {
            var message = result.Messages.First();
            var contentItem = message.Contents.SingleOrDefault();
            content = content.Substring(startPos);

            foreach (var toolCallJson in content.Split(["<tool_call_json>"], StringSplitOptions.None))
            {
                var toolCall = toolCallJson.Trim();
                if (toolCall.Length == 0)
                {
                    continue;
                }

                var endPos = toolCall.IndexOf("</tool", StringComparison.Ordinal);
                if (endPos > 0)
                {
                    toolCall = toolCall.Substring(0, endPos);
                    try
                    {
                        // Deserialize just the first. We don't care if there are trailing braces etc.
                        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(toolCall));
                        var toolCallParsed = JsonSerializer.Deserialize<ToolCall>(ref reader, _jsonOptions);
                        if (!string.IsNullOrEmpty(toolCallParsed?.Name))
                        {
                            if (toolCallParsed!.Arguments is not null)
                            {
                                ParseArguments(toolCallParsed.Arguments);
                            }

                            var id = Guid.NewGuid().ToString().Substring(0, 6);
                            message.Contents.Add(new FunctionCallContent(id, toolCallParsed.Name!, toolCallParsed.Arguments is { } args ? new ReadOnlyDictionary<string, object?>(args) : null));

                            if (contentItem is not null)
                            {
                                message.Contents.Remove(contentItem);
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore invalid tool calls
                    }
                }
            }
        }

        return result;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    private static void ParseArguments(IDictionary<string, object?> arguments)
    {
        // This is a simple implementation. A more robust answer is to use other schema information given by
        // the AIFunction here, as for example is done in OpenAIChatClient.
        foreach (var kvp in arguments.ToArray())
        {
            if (kvp.Value is JsonElement jsonElement)
            {
                arguments[kvp.Key] = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString(),
                    JsonValueKind.Number => jsonElement.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => jsonElement.ToString()
                };
            }
        }
    }

    private static ChatMessage CreateToolPrompt(IList<AITool> tools)
    {
        var toolDescriptorsJson = JsonSerializer.Serialize(tools.OfType<AIFunction>().Select(t => t.JsonSchema), _jsonOptions);
        var prompt = $$"""
            {{MessageIntro}}

            For each function call, return a JSON object with the function name and arguments within <tool_call_json></tool_call_json> XML tags
            as follows:
            <tool_calls>
              <tool_call_json>{"name": "tool_name", "arguments": { argname1: argval1, argname2: argval2, ... } }</tool_call_json>
            </tool_calls>
            Note that the contents of <tool_call_json></tool_call_json> MUST be a valid JSON object, with no other text.

            Once you receive the result as a JSON object within <tool_call_result></tool_call_result> XML tags, use it to
            answer the user's question without repeating the same tool call.

            Here are the available tools:
            <tools>{{toolDescriptorsJson}}</tools>
            """;
        return new ChatMessage(ChatRole.System, prompt);
    }

    private sealed class ToolCall
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public IDictionary<string, object?>? Arguments { get; set; }
    }

    private sealed class ToolCallResult
    {
        public string? Id { get; set; }
        public object? Result { get; set; }
    }
}

public static class PromptBasedFunctionCallingChatClientExtensions
{
    public static ChatClientBuilder UsePromptBasedFunctionCalling(this ChatClientBuilder builder)
        => builder.Use(innerClient => new PromptBasedFunctionCallingChatClient(innerClient));
}

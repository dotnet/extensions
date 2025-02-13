// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable S1144 // Unused private types or members should be removed
#pragma warning disable S3459 // Unassigned members should be removed

namespace Microsoft.Extensions.AI;

// This isn't a feature we're planning to ship, but demonstrates how custom clients can
// layer in non-trivial functionality. In this case we're able to upgrade non-function-calling models
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

    public override async Task<ChatResponse> GetResponseAsync(IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        // Our goal is to convert tools into a prompt describing them, then to detect tool calls in the
        // response and convert those into FunctionCallContent.
        if (options?.Tools is { Count: > 0 })
        {
            AddOrUpdateToolPrompt(chatMessages, options.Tools);
            options = options.Clone();
            options.Tools = null;

            options.StopSequences ??= [];
            if (!options.StopSequences.Contains("</tool_calls>"))
            {
                options.StopSequences.Add("</tool_calls>");
            }

            // Since the point of this client is to avoid relying on the underlying model having
            // native tool call support, we have to replace any "tool" or "toolcall" messages with
            // "user" or "assistant" ones.
            foreach (var message in chatMessages)
            {
                for (var itemIndex = 0; itemIndex < message.Contents.Count; itemIndex++)
                {
                    if (message.Contents[itemIndex] is FunctionResultContent frc)
                    {
                        var toolCallResultJson = JsonSerializer.Serialize(new ToolCallResult { Id = frc.CallId, Result = frc.Result }, _jsonOptions);
                        message.Role = ChatRole.User;
                        message.Contents[itemIndex] = new TextContent(
                            $"<tool_call_result>{toolCallResultJson}</tool_call_result>");
                    }
                    else if (message.Contents[itemIndex] is FunctionCallContent fcc)
                    {
                        var toolCallJson = JsonSerializer.Serialize(new { fcc.CallId, fcc.Name, fcc.Arguments }, _jsonOptions);
                        message.Role = ChatRole.Assistant;
                        message.Contents[itemIndex] = new TextContent(
                            $"<tool_call_json>{toolCallJson}</tool_call_json>");
                    }
                }
            }
        }

        var result = await base.GetResponseAsync(chatMessages, options, cancellationToken);

        if (result.Choices.FirstOrDefault()?.Text is { } content && content.IndexOf("<tool_call_json>", StringComparison.Ordinal) is int startPos
            && startPos >= 0)
        {
            var message = result.Choices.First();
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
                        var toolCallParsed = JsonSerializer.Deserialize<ToolCall>(toolCall, _jsonOptions);
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

    private static void AddOrUpdateToolPrompt(IList<ChatMessage> chatMessages, IList<AITool> tools)
    {
        var existingToolPrompt = chatMessages.FirstOrDefault(c => c.Text?.StartsWith(MessageIntro, StringComparison.Ordinal) is true);
        if (existingToolPrompt is null)
        {
            existingToolPrompt = new ChatMessage(ChatRole.System, (string?)null);
            chatMessages.Insert(0, existingToolPrompt);
        }

        var toolDescriptorsJson = JsonSerializer.Serialize(tools.OfType<AIFunction>().Select(ToToolDescriptor), _jsonOptions);
        existingToolPrompt.Text = $$"""
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
    }

    private static ToolDescriptor ToToolDescriptor(AIFunction tool) => new()
    {
        Name = tool.Name,
        Description = tool.Description,
        Arguments = tool.UnderlyingMethod?.GetParameters().ToDictionary(
            p => p.Name!,
            p => new ToolParameterDescriptor
            {
                Type = p.Name!,
                Description = p.GetCustomAttribute<DescriptionAttribute>()?.Description,
                Enum = p.ParameterType.IsEnum ? Enum.GetNames(p.ParameterType) : null,
                Required = !p.IsOptional,
            }) ?? [],
    };

    private sealed class ToolDescriptor
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public IDictionary<string, ToolParameterDescriptor>? Arguments { get; set; }
    }

    private sealed class ToolParameterDescriptor
    {
        public string? Type { get; set; }
        public string? Description { get; set; }
        public bool? Required { get; set; }
        public string[]? Enum { get; set; }
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

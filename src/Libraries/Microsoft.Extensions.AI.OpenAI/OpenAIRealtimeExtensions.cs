// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.RealtimeConversation;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for working with <see cref="RealtimeConversationSession"/> and related types.
/// </summary>
public static class OpenAIRealtimeExtensions
{
    /// <summary>
    /// Converts a <see cref="AIFunction"/> into a <see cref="ConversationFunctionTool"/> so that
    /// it can be used with <see cref="RealtimeConversationClient"/>.
    /// </summary>
    /// <returns>A <see cref="ConversationFunctionTool"/> that can be used with <see cref="RealtimeConversationClient"/>.</returns>
    public static ConversationFunctionTool ToConversationFunctionTool(this AIFunction aiFunction)
    {
        _ = Throw.IfNull(aiFunction);

        ConversationFunctionToolParametersSchema functionToolSchema = JsonSerializer.Deserialize(aiFunction.Metadata.Schema, OpenAIJsonContext.Default.ConversationFunctionToolParametersSchema)!;
        BinaryData functionParameters = new(JsonSerializer.SerializeToUtf8Bytes(functionToolSchema, OpenAIJsonContext.Default.ConversationFunctionToolParametersSchema));
        return new ConversationFunctionTool
        {
            Name = aiFunction.Metadata.Name,
            Description = aiFunction.Metadata.Description,
            Parameters = functionParameters
        };
    }

    /// <summary>
    /// Handles tool calls.
    ///
    /// If the <paramref name="update"/> represents a tool call, calls the corresponding tool and
    /// adds the result to the <paramref name="session"/>.
    ///
    /// If the <paramref name="update"/> represents the end of a response, checks if this was due
    /// to a tool call and if so, instructs the <paramref name="session"/> to begin responding to it.
    /// </summary>
    /// <param name="session">The <see cref="RealtimeConversationSession"/>.</param>
    /// <param name="update">The <see cref="ConversationUpdate"/> being processed.</param>
    /// <param name="tools">The available tools.</param>
    /// <param name="detailedErrors">An optional flag specifying whether to disclose detailed exception information to the model. The default value is <see langword="false"/>.</param>
    /// <param name="jsonSerializerOptions">An optional <see cref="JsonSerializerOptions"/> that controls JSON handling.</param>
    /// <param name="cancellationToken">An optional <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> that represents the completion of processing, including invoking any asynchronous tools.</returns>
    public static async Task HandleToolCallsAsync(
        this RealtimeConversationSession session,
        ConversationUpdate update,
        IReadOnlyList<AIFunction> tools,
        bool? detailedErrors = false,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(session);
        _ = Throw.IfNull(update);
        _ = Throw.IfNull(tools);

        if (update is ConversationItemStreamingFinishedUpdate itemFinished)
        {
            // If we need to call a tool to update the model, do so
            if (!string.IsNullOrEmpty(itemFinished.FunctionName)
                && await itemFinished.GetFunctionCallOutputAsync(tools, detailedErrors, jsonSerializerOptions, cancellationToken).ConfigureAwait(false) is { } output)
            {
                await session.AddItemAsync(output, cancellationToken).ConfigureAwait(false);
            }
        }
        else if (update is ConversationResponseFinishedUpdate responseFinished)
        {
            // If we added one or more function call results, instruct the model to respond to them
            if (responseFinished.CreatedItems.Any(item => !string.IsNullOrEmpty(item.FunctionName)))
            {
                await session!.StartResponseAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static async Task<ConversationItem?> GetFunctionCallOutputAsync(
        this ConversationItemStreamingFinishedUpdate update,
        IReadOnlyList<AIFunction> tools,
        bool? detailedErrors = false,
        JsonSerializerOptions? jsonSerializerOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(update.FunctionName)
            && tools.FirstOrDefault(t => t.Metadata.Name == update.FunctionName) is AIFunction aiFunction)
        {
            var jsonOptions = jsonSerializerOptions ?? AIJsonUtilities.DefaultOptions;

            var functionCallContent = FunctionCallContent.CreateFromParsedArguments(
                update.FunctionCallArguments, update.FunctionCallId, update.FunctionName,
                    argumentParser: json => JsonSerializer.Deserialize(json,
                    (JsonTypeInfo<IDictionary<string, object>>)jsonOptions.GetTypeInfo(typeof(IDictionary<string, object>)))!);

            try
            {
                var result = await aiFunction.InvokeAsync(functionCallContent.Arguments, cancellationToken).ConfigureAwait(false);
                var resultJson = JsonSerializer.Serialize(result, jsonOptions.GetTypeInfo(typeof(object)));
                return ConversationItem.CreateFunctionCallOutput(update.FunctionCallId, resultJson);
            }
            catch (JsonException)
            {
                return ConversationItem.CreateFunctionCallOutput(update.FunctionCallId, "Invalid JSON");
            }
            catch (Exception e) when (!cancellationToken.IsCancellationRequested)
            {
                var message = "Error calling tool";

                if (detailedErrors == true)
                {
                    message += $": {e.Message}";
                }

                return ConversationItem.CreateFunctionCallOutput(update.FunctionCallId, message);
            }
        }

        return null;
    }

    internal sealed class ConversationFunctionToolParametersSchema
    {
        public string? Type { get; set; }
        public IDictionary<string, JsonElement>? Properties { get; set; }
        public IEnumerable<string>? Required { get; set; }
    }
}

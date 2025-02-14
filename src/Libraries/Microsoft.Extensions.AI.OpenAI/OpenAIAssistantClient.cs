// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI;
using OpenAI.Assistants;
using OpenAI.Chat;

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable CA1031 // Do not catch general exception types
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S1751 // Loops with at most one iteration should be refactored
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1108 // Block statements should not contain embedded comments

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an OpenAI <see cref="OpenAIClient"/> or <see cref="ChatClient"/>.</summary>
internal sealed class OpenAIAssistantClient : IChatClient
{
    /// <summary>Metadata for the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The underlying <see cref="AssistantClient" />.</summary>
    private readonly AssistantClient _assistantClient;

    /// <summary>The ID of the assistant to use.</summary>
    private readonly string _assistantId;

    /// <summary>The thread ID to use if none is supplied in <see cref="ChatOptions.ChatThreadId"/>.</summary>
    private readonly string? _threadId;

    /// <summary>Initializes a new instance of the <see cref="OpenAIAssistantClient"/> class for the specified <see cref="AssistantClient"/>.</summary>
    /// <param name="assistantClient">The underlying client.</param>
    /// <param name="assistantId">The ID of the assistant to use.</param>
    /// <param name="threadId">
    /// The ID of the thread to use. If not supplied here, it should be supplied per request in <see cref="ChatOptions.ChatThreadId"/>.
    /// If none is supplied, a new thread will be created for a request.
    /// </param>
    public OpenAIAssistantClient(AssistantClient assistantClient, string assistantId, string? threadId)
    {
        _assistantClient = Throw.IfNull(assistantClient);
        _assistantId = Throw.IfNull(assistantId);
        _threadId = threadId;

        _metadata = new("openai");
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(ChatClientMetadata) ? _metadata :
            serviceType == typeof(AssistantClient) ? _assistantClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        GetStreamingResponseAsync(chatMessages, options, cancellationToken).ToChatResponseAsync(coalesceContent: true, cancellationToken);

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IList<ChatMessage> chatMessages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Extract necessary state from chatMessages and options.
        (RunCreationOptions runOptions, List<FunctionResultContent>? toolResults) = CreateRunOptions(chatMessages, options);

        // Get the thread ID.
        string? threadId = options?.ChatThreadId ?? _threadId;
        if (threadId is null && toolResults is not null)
        {
            Throw.ArgumentException(nameof(chatMessages), "No thread ID was provided, but chat messages includes tool results.");
        }

        // Get the updates to process from the assistant. If we have any tool results, this means submitting those and ignoring
        // our runOptions. Otherwise, create a run, and a thread if we don't have one.
        IAsyncEnumerable<StreamingUpdate> updates;
        if (GetRunId(toolResults, out List<ToolOutput>? toolOutputs) is string existingRunId)
        {
            updates = _assistantClient.SubmitToolOutputsToRunStreamingAsync(threadId, existingRunId, toolOutputs, cancellationToken);
        }
        else if (threadId is null)
        {
            ThreadCreationOptions creationOptions = new();
            foreach (var message in runOptions.AdditionalMessages)
            {
                creationOptions.InitialMessages.Add(message);
            }

            runOptions.AdditionalMessages.Clear();

            updates = _assistantClient.CreateThreadAndRunStreamingAsync(_assistantId, creationOptions, runOptions, cancellationToken: cancellationToken);
        }
        else
        {
            updates = _assistantClient.CreateRunStreamingAsync(threadId, _assistantId, runOptions, cancellationToken);
        }

        // Process each update.
        await foreach (var update in updates.ConfigureAwait(false))
        {
            switch (update)
            {
                case MessageContentUpdate mcu:
                    yield return new()
                    {
                        ChatThreadId = threadId,
                        RawRepresentation = mcu,
                        Role = mcu.Role == MessageRole.User ? ChatRole.User : ChatRole.Assistant,
                        Text = mcu.Text,
                    };
                    break;

                case ThreadUpdate tu when options is not null:
                    threadId ??= tu.Value.Id;
                    break;

                case RunUpdate ru:
                    threadId ??= ru.Value.ThreadId;

                    ChatResponseUpdate ruUpdate = new()
                    {
                        AuthorName = ru.Value.AssistantId,
                        ChatThreadId = threadId,
                        CreatedAt = ru.Value.CreatedAt,
                        ModelId = ru.Value.Model,
                        RawRepresentation = ru,
                        ResponseId = ru.Value.Id,
                        Role = ChatRole.Assistant,
                    };

                    if (ru.Value.Usage is { } usage)
                    {
                        ruUpdate.Contents.Add(new UsageContent(new()
                        {
                            InputTokenCount = usage.InputTokenCount,
                            OutputTokenCount = usage.OutputTokenCount,
                            TotalTokenCount = usage.TotalTokenCount,
                        }));
                    }

                    if (ru is RequiredActionUpdate rau && rau.ToolCallId is string toolCallId && rau.FunctionName is string functionName)
                    {
                        ruUpdate.Contents.Add(
                            new FunctionCallContent(
                                JsonSerializer.Serialize(new[] { ru.Value.Id, toolCallId }, OpenAIJsonContext.Default.StringArray!),
                                functionName,
                                JsonSerializer.Deserialize(rau.FunctionArguments, OpenAIJsonContext.Default.IDictionaryStringObject)!));
                    }

                    yield return ruUpdate;
                    break;
            }
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IChatClient interface.
    }

    /// <summary>Adds the provided messages to the thread and returns the options to use for the request.</summary>
    private static (RunCreationOptions RunOptions, List<FunctionResultContent>? ToolResults) CreateRunOptions(IList<ChatMessage> chatMessages, ChatOptions? options)
    {
        _ = Throw.IfNull(chatMessages);

        RunCreationOptions runOptions = new();

        // Handle ChatOptions.
        if (options is not null)
        {
            // Propagate the simple properties that have a 1:1 correspondence.
            runOptions.MaxOutputTokenCount = options.MaxOutputTokens;
            runOptions.ModelOverride = options.ModelId;
            runOptions.NucleusSamplingFactor = options.TopP;
            runOptions.Temperature = options.Temperature;

            // Propagate additional properties from AdditionalProperties.
            if (options.AdditionalProperties?.TryGetValue(nameof(RunCreationOptions.AllowParallelToolCalls), out bool allowParallelToolCalls) is true)
            {
                runOptions.AllowParallelToolCalls = allowParallelToolCalls;
            }

            if (options.AdditionalProperties?.TryGetValue(nameof(RunCreationOptions.MaxInputTokenCount), out int maxInputTokenCount) is true)
            {
                runOptions.MaxInputTokenCount = maxInputTokenCount;
            }

            if (options.AdditionalProperties?.TryGetValue(nameof(RunCreationOptions.TruncationStrategy), out RunTruncationStrategy? truncationStrategy) is true)
            {
                runOptions.TruncationStrategy = truncationStrategy;
            }

            // Store all the tools to use.
            if (options.Tools is { Count: > 0 } tools)
            {
                foreach (AITool tool in tools)
                {
                    if (tool is AIFunction aiFunction)
                    {
                        bool? strict =
                            aiFunction.AdditionalProperties.TryGetValue("Strict", out object? strictObj) &&
                            strictObj is bool strictValue ?
                            strictValue : null;

                        var functionParameters = BinaryData.FromBytes(
                            JsonSerializer.SerializeToUtf8Bytes(
                                JsonSerializer.Deserialize(aiFunction.JsonSchema, OpenAIJsonContext.Default.OpenAIChatToolJson)!,
                                OpenAIJsonContext.Default.OpenAIChatToolJson));

                        runOptions.ToolsOverride.Add(ToolDefinition.CreateFunction(aiFunction.Name, aiFunction.Description, functionParameters, strict));
                    }
                }
            }

            // Store the tool mode.
            switch (options.ToolMode)
            {
                case NoneChatToolMode:
                    runOptions.ToolConstraint = ToolConstraint.None;
                    break;

                case null:
                case AutoChatToolMode:
                    runOptions.ToolConstraint = ToolConstraint.Auto;
                    break;

                case RequiredChatToolMode required:
                    runOptions.ToolConstraint = required.RequiredFunctionName is null ?
                        new ToolConstraint(ToolDefinition.CreateFunction(required.RequiredFunctionName)) :
                        ToolConstraint.Required;
                    break;
            }

            // Store the response format.
            if (options.ResponseFormat is ChatResponseFormatText)
            {
                runOptions.ResponseFormat = AssistantResponseFormat.Text;
            }
            else if (options.ResponseFormat is ChatResponseFormatJson jsonFormat)
            {
                runOptions.ResponseFormat = jsonFormat.Schema is { } jsonSchema ?
                    AssistantResponseFormat.CreateJsonSchemaFormat(
                        jsonFormat.SchemaName ?? "json_schema",
                        BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(jsonSchema, OpenAIJsonContext.Default.JsonElement)),
                        jsonFormat.SchemaDescription) :
                    AssistantResponseFormat.JsonObject;
            }
        }

        // Handle ChatMessages. System messages are turned into additional instructions.
        StringBuilder? instructions = null;
        List<FunctionResultContent>? functionResults = null;
        foreach (var chatMessage in chatMessages)
        {
            List<MessageContent> messageContents = [];

            if (chatMessage.Role == ChatRole.System ||
                chatMessage.Role == OpenAIModelMappers.ChatRoleDeveloper)
            {
                instructions ??= new();
                foreach (var textContent in chatMessage.Contents.OfType<TextContent>())
                {
                    _ = instructions.Append(textContent);
                }

                continue;
            }

            foreach (AIContent content in chatMessage.Contents)
            {
                switch (content)
                {
                    case TextContent tc:
                        messageContents.Add(MessageContent.FromText(tc.Text));
                        break;

                    case DataContent dc when dc.MediaTypeStartsWith("image/"):
                        messageContents.Add(MessageContent.FromImageUri(new(dc.Uri)));
                        break;

                    case FunctionResultContent frc:
                        (functionResults ??= []).Add(frc);
                        break;
                }
            }

            if (messageContents.Count > 0)
            {
                runOptions.AdditionalMessages.Add(new(
                    chatMessage.Role == ChatRole.Assistant ? MessageRole.Assistant : MessageRole.User,
                    messageContents));
            }
        }

        if (instructions is not null)
        {
            runOptions.AdditionalInstructions = instructions.ToString();
        }

        return (runOptions, functionResults);
    }

    private static string? GetRunId(List<FunctionResultContent>? toolResults, out List<ToolOutput>? toolOutputs)
    {
        string? runId = null;
        toolOutputs = null;
        if (toolResults?.Count > 0)
        {
            foreach (var frc in toolResults)
            {
                // When creating the FunctionCallContext, we created it with a CallId == [runId, callId].
                // We need to extract the run ID and ensure that the ToolOutput we send back to OpenAI
                // is only the call ID.
                string[]? runAndCallIDs;
                try
                {
                    runAndCallIDs = JsonSerializer.Deserialize(frc.CallId, OpenAIJsonContext.Default.StringArray);
                }
                catch
                {
                    continue;
                }

                if (runAndCallIDs is null ||
                    runAndCallIDs.Length != 2 ||
                    string.IsNullOrWhiteSpace(runAndCallIDs[0]) || // run ID
                    string.IsNullOrWhiteSpace(runAndCallIDs[1]) || // call ID
                    (runId is not null && runId != runAndCallIDs[0]))
                {
                    continue;
                }

                runId = runAndCallIDs[0];
                (toolOutputs ??= []).Add(new(runAndCallIDs[1], frc.Result?.ToString() ?? string.Empty));
            }
        }

        return runId;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using OpenAI.Assistants;

#pragma warning disable SA1005 // Single line comments should begin with single space
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable S125 // Sections of code should not be commented out
#pragma warning disable S1751 // Loops with at most one iteration should be refactored
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an OpenAI <see cref="AssistantClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIOpenAIAssistants)]
internal sealed class OpenAIAssistantsChatClient : IChatClient
{
    /// <summary>The underlying <see cref="AssistantClient" />.</summary>
    private readonly AssistantClient _client;

    /// <summary>Metadata for the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The ID of the agent to use.</summary>
    private readonly string _assistantId;

    /// <summary>The thread ID to use if none is supplied in <see cref="ChatOptions.ConversationId"/>.</summary>
    private readonly string? _defaultThreadId;

    /// <summary>List of tools associated with the assistant.</summary>
    private IReadOnlyList<ToolDefinition>? _assistantTools;

    /// <summary>Initializes a new instance of the <see cref="OpenAIAssistantsChatClient"/> class for the specified <see cref="AssistantClient"/>.</summary>
    public OpenAIAssistantsChatClient(AssistantClient assistantClient, string assistantId, string? defaultThreadId)
    {
        _client = Throw.IfNull(assistantClient);
        _assistantId = Throw.IfNullOrWhitespace(assistantId);
        _defaultThreadId = defaultThreadId;

        _metadata = new("openai", assistantClient.Endpoint);
    }

    /// <summary>Initializes a new instance of the <see cref="OpenAIAssistantsChatClient"/> class for the specified <see cref="AssistantClient"/>.</summary>
    public OpenAIAssistantsChatClient(AssistantClient assistantClient, Assistant assistant, string? defaultThreadId)
        : this(assistantClient, Throw.IfNull(assistant).Id, defaultThreadId)
    {
        _assistantTools = assistant.Tools;
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType is null ? throw new ArgumentNullException(nameof(serviceType)) :
        serviceKey is not null ? null :
        serviceType == typeof(ChatClientMetadata) ? _metadata :
        serviceType == typeof(AssistantClient) ? _client :
        serviceType.IsInstanceOfType(this) ? this :
        null;

    /// <inheritdoc />
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        GetStreamingResponseAsync(messages, options, cancellationToken).ToChatResponseAsync(cancellationToken);

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        // Extract necessary state from messages and options.
        (RunCreationOptions runOptions, ToolResources? toolResources, List<FunctionResultContent>? toolResults) = await CreateRunOptionsAsync(messages, options, cancellationToken).ConfigureAwait(false);

        // Get the thread ID.
        string? threadId = options?.ConversationId ?? _defaultThreadId;

        // Get any active run ID for this thread. This is necessary in case a thread has been left with an
        // active run, in which case all attempts other than submitting tools will fail. We thus need to cancel
        // any active run on the thread if we're not submitting tool results to it.
        ThreadRun? threadRun = null;
        if (threadId is not null)
        {
            await foreach (var run in _client.GetRunsAsync(
                threadId,
                new RunCollectionOptions { Order = RunCollectionOrder.Descending, PageSizeLimit = 1 },
                cancellationToken: cancellationToken).ConfigureAwait(false))
            {
                if (run.Status != RunStatus.Completed && run.Status != RunStatus.Cancelled && run.Status != RunStatus.Failed && run.Status != RunStatus.Expired)
                {
                    threadRun = run;
                }

                break;
            }
        }

        // Submit the request.
        IAsyncEnumerable<StreamingUpdate> updates;
        if (threadRun is not null &&
            ConvertFunctionResultsToToolOutput(toolResults, out List<ToolOutput>? toolOutputs) is { } toolRunId &&
            toolRunId == threadRun.Id)
        {
            // There's an active run and, critically, we have tool results to submit for that exact run, so submit the results and continue streaming.
            // This is going to ignore any additional messages in the run options, as we are only submitting tool outputs,
            // but there doesn't appear to be a way to submit additional messages, and having such additional messages is rare.
            updates = _client.SubmitToolOutputsToRunStreamingAsync(threadRun.ThreadId, threadRun.Id, toolOutputs, cancellationToken);
        }
        else
        {
            if (threadId is null)
            {
                // No thread ID was provided, so create a new thread.
                ThreadCreationOptions threadCreationOptions = new()
                {
                    ToolResources = toolResources,
                };

                foreach (var message in runOptions.AdditionalMessages)
                {
                    threadCreationOptions.InitialMessages.Add(message);
                }

                runOptions.AdditionalMessages.Clear();

                var thread = await _client.CreateThreadAsync(threadCreationOptions, cancellationToken).ConfigureAwait(false);
                threadId = thread.Value.Id;
            }
            else if (threadRun is not null)
            {
                // There was an active run; we need to cancel it before starting a new run.
                _ = await _client.CancelRunAsync(threadId, threadRun.Id, cancellationToken).ConfigureAwait(false);
                threadRun = null;
            }

            // Now create a new run and stream the results.
            updates = _client.CreateRunStreamingAsync(
                threadId: threadId,
                _assistantId,
                runOptions,
                cancellationToken);
        }

        // Process each update.
        string? responseId = null;
        await foreach (var update in updates.ConfigureAwait(false))
        {
            switch (update)
            {
                case ThreadUpdate tu:
                    threadId ??= tu.Value.Id;
                    goto default;

                case RunUpdate ru:
                    threadId ??= ru.Value.ThreadId;
                    responseId ??= ru.Value.Id;

                    ChatResponseUpdate ruUpdate = new()
                    {
                        AuthorName = _assistantId,
                        ConversationId = threadId,
                        CreatedAt = ru.Value.CreatedAt,
                        MessageId = responseId,
                        ModelId = ru.Value.Model,
                        RawRepresentation = ru,
                        ResponseId = responseId,
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
                        var fcc = OpenAIClientExtensions.ParseCallContent(
                            rau.FunctionArguments,
                            JsonSerializer.Serialize([ru.Value.Id, toolCallId], OpenAIJsonContext.Default.StringArray),
                            functionName);
                        fcc.RawRepresentation = ru;
                        ruUpdate.Contents.Add(fcc);
                    }

                    yield return ruUpdate;
                    break;

                case RunStepDetailsUpdate details:
                    if (!string.IsNullOrEmpty(details.CodeInterpreterInput))
                    {
                        CodeInterpreterToolCallContent hcitcc = new(details.ToolCallId)
                        {
                            Inputs = [new DataContent(Encoding.UTF8.GetBytes(details.CodeInterpreterInput), OpenAIClientExtensions.PythonMediaType)],
                            RawRepresentation = details,
                        };

                        yield return new ChatResponseUpdate(ChatRole.Assistant, [hcitcc])
                        {
                            AuthorName = _assistantId,
                            ConversationId = threadId,
                            MessageId = responseId,
                            RawRepresentation = update,
                            ResponseId = responseId,
                        };
                    }

                    if (details.CodeInterpreterOutputs is { Count: > 0 })
                    {
                        CodeInterpreterToolResultContent hcitrc = new(details.ToolCallId)
                        {
                            RawRepresentation = details,
                        };

                        foreach (var output in details.CodeInterpreterOutputs)
                        {
                            if (output.ImageFileId is not null)
                            {
                                (hcitrc.Outputs ??= []).Add(new HostedFileContent(output.ImageFileId) { MediaType = "image/*" });
                            }

                            if (output.Logs is string logs)
                            {
                                (hcitrc.Outputs ??= []).Add(new TextContent(logs));
                            }
                        }

                        yield return new ChatResponseUpdate(ChatRole.Assistant, [hcitrc])
                        {
                            AuthorName = _assistantId,
                            ConversationId = threadId,
                            MessageId = responseId,
                            RawRepresentation = update,
                            ResponseId = responseId,
                        };
                    }
                    break;

                case MessageContentUpdate mcu:
                    ChatResponseUpdate textUpdate = new(mcu.Role == MessageRole.User ? ChatRole.User : ChatRole.Assistant, mcu.Text)
                    {
                        AuthorName = _assistantId,
                        ConversationId = threadId,
                        MessageId = responseId,
                        RawRepresentation = mcu,
                        ResponseId = responseId,
                    };

                    // Add any annotations from the text update. The OpenAI Assistants API does not support passing these back
                    // into the model (MessageContent.FromXx does not support providing annotations), so they end up being one way and are dropped
                    // on subsequent requests.
                    if (mcu.TextAnnotation is { } tau)
                    {
                        string? fileId = null;
                        string? toolName = null;
                        if (!string.IsNullOrWhiteSpace(tau.InputFileId))
                        {
                            fileId = tau.InputFileId;
                            toolName = "file_search";
                        }
                        else if (!string.IsNullOrWhiteSpace(tau.OutputFileId))
                        {
                            fileId = tau.OutputFileId;
                            toolName = "code_interpreter";
                        }

                        if (fileId is not null)
                        {
                            if (textUpdate.Contents.Count == 0)
                            {
                                // In case a chunk doesn't have text content, create one with empty text to hold the annotation.
                                textUpdate.Contents.Add(new TextContent(string.Empty));
                            }

                            (((TextContent)textUpdate.Contents[0]).Annotations ??= []).Add(new CitationAnnotation
                            {
                                RawRepresentation = tau,
                                AnnotatedRegions = [new TextSpanAnnotatedRegion { StartIndex = tau.StartIndex, EndIndex = tau.EndIndex }],
                                FileId = fileId,
                                ToolName = toolName,
                            });
                        }
                    }

                    yield return textUpdate;
                    break;

                default:
                    yield return new()
                    {
                        AuthorName = _assistantId,
                        ConversationId = threadId,
                        MessageId = responseId,
                        RawRepresentation = update,
                        ResponseId = responseId,
                        Role = ChatRole.Assistant,
                    };
                    break;
            }
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // nop
    }

    /// <summary>Converts an Extensions function to an OpenAI assistants function tool.</summary>
    internal static FunctionToolDefinition ToOpenAIAssistantsFunctionToolDefinition(AIFunctionDeclaration aiFunction, ChatOptions? options = null)
    {
        bool? strict =
            OpenAIClientExtensions.HasStrict(aiFunction.AdditionalProperties) ??
            OpenAIClientExtensions.HasStrict(options?.AdditionalProperties);

        return new FunctionToolDefinition(aiFunction.Name)
        {
            Description = aiFunction.Description,
            Parameters = OpenAIClientExtensions.ToOpenAIFunctionParameters(aiFunction, strict),
            StrictParameterSchemaEnabled = strict,
        };
    }

    /// <summary>
    /// Creates the <see cref="RunCreationOptions"/> to use for the request and extracts any function result contents 
    /// that need to be submitted as tool results.
    /// </summary>
    private async ValueTask<(RunCreationOptions RunOptions, ToolResources? Resources, List<FunctionResultContent>? ToolResults)> CreateRunOptionsAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellationToken)
    {
        // Create the options instance to populate, either a fresh or using one the caller provides.
        RunCreationOptions runOptions =
            options?.RawRepresentationFactory?.Invoke(this) as RunCreationOptions ??
            new();

        ToolResources? resources = null;

        // Populate the run options from the ChatOptions, if provided.
        if (options is not null)
        {
            runOptions.MaxOutputTokenCount ??= options.MaxOutputTokens;
            runOptions.ModelOverride ??= options.ModelId;
            runOptions.NucleusSamplingFactor ??= options.TopP;
            runOptions.Temperature ??= options.Temperature;
            runOptions.AllowParallelToolCalls ??= options.AllowMultipleToolCalls;

            if (options.Tools is { Count: > 0 } tools)
            {
                HashSet<ToolDefinition> toolsOverride = new(ToolDefinitionNameEqualityComparer.Instance);

                // If the caller has provided any tool overrides, we'll assume they don't want to use the assistant's tools.
                // But if they haven't, the only way we can provide our tools is via an override, whereas we'd really like to
                // just add them. To handle that, we'll get all of the assistant's tools and add them to the override list
                // along with our tools.
                if (runOptions.ToolsOverride.Count == 0)
                {
                    if (_assistantTools is null)
                    {
                        var assistant = await _client.GetAssistantAsync(_assistantId, cancellationToken).ConfigureAwait(false);
                        _assistantTools = assistant.Value.Tools;
                    }

                    toolsOverride.UnionWith(_assistantTools);
                }

                // The caller can provide tools in the supplied ThreadAndRunOptions. Augment it with any supplied via ChatOptions.Tools.
                foreach (AITool tool in tools)
                {
                    switch (tool)
                    {
                        case AIFunctionDeclaration aiFunction:
                            _ = toolsOverride.Add(ToOpenAIAssistantsFunctionToolDefinition(aiFunction, options));
                            break;

                        case HostedCodeInterpreterTool codeInterpreterTool:
                            var interpreterToolDef = ToolDefinition.CreateCodeInterpreter();
                            _ = toolsOverride.Add(interpreterToolDef);

                            if (codeInterpreterTool.Inputs?.Count is > 0)
                            {
                                ThreadInitializationMessage? threadInitializationMessage = null;
                                foreach (var input in codeInterpreterTool.Inputs)
                                {
                                    if (input is HostedFileContent hostedFile)
                                    {
                                        threadInitializationMessage ??= new(MessageRole.User, [MessageContent.FromText("attachments")]);
                                        threadInitializationMessage.Attachments.Add(new(hostedFile.FileId, [interpreterToolDef]));
                                    }
                                }

                                if (threadInitializationMessage is not null)
                                {
                                    runOptions.AdditionalMessages.Add(threadInitializationMessage);
                                }
                            }

                            break;

                        case HostedFileSearchTool fileSearchTool:
                            var fst = ToolDefinition.CreateFileSearch(fileSearchTool.MaximumResultCount);
                            fst.RankingOptions = fileSearchTool.GetProperty<FileSearchRankingOptions>(nameof(FileSearchToolDefinition.RankingOptions));
                            _ = toolsOverride.Add(fst);

                            if (fileSearchTool.Inputs is { Count: > 0 } fileSearchInputs)
                            {
                                foreach (var input in fileSearchInputs)
                                {
                                    if (input is HostedVectorStoreContent file)
                                    {
                                        (resources ??= new()).FileSearch ??= new();
                                        resources.FileSearch.VectorStoreIds.Add(file.VectorStoreId);
                                    }
                                }
                            }

                            break;
                    }
                }

                foreach (var tool in toolsOverride)
                {
                    runOptions.ToolsOverride.Add(tool);
                }
            }

            // Store the tool mode, if relevant.
            if (runOptions.ToolConstraint is null)
            {
                switch (options.ToolMode)
                {
                    case NoneChatToolMode:
                        runOptions.ToolConstraint = ToolConstraint.None;
                        break;

                    case AutoChatToolMode:
                        runOptions.ToolConstraint = ToolConstraint.Auto;
                        break;

                    case RequiredChatToolMode required when required.RequiredFunctionName is { } functionName:
                        runOptions.ToolConstraint = new ToolConstraint(ToolDefinition.CreateFunction(functionName));
                        break;

                    case RequiredChatToolMode required:
                        runOptions.ToolConstraint = ToolConstraint.Required;
                        break;
                }
            }

            // Store the response format, if relevant.
            if (runOptions.ResponseFormat is null)
            {
                switch (options.ResponseFormat)
                {
                    case ChatResponseFormatText:
                        runOptions.ResponseFormat = AssistantResponseFormat.CreateTextFormat();
                        break;

                    case ChatResponseFormatJson jsonFormat when OpenAIClientExtensions.StrictSchemaTransformCache.GetOrCreateTransformedSchema(jsonFormat) is { } jsonSchema:
                        runOptions.ResponseFormat = AssistantResponseFormat.CreateJsonSchemaFormat(
                            jsonFormat.SchemaName,
                            BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(jsonSchema, OpenAIJsonContext.Default.JsonElement)),
                            jsonFormat.SchemaDescription,
                            OpenAIClientExtensions.HasStrict(options.AdditionalProperties));
                        break;

                    case ChatResponseFormatJson jsonFormat:
                        runOptions.ResponseFormat = AssistantResponseFormat.CreateJsonObjectFormat();
                        break;
                }
            }
        }

        // Configure system instructions.
        StringBuilder? instructions = null;
        void AppendSystemInstructions(string? toAppend)
        {
            if (!string.IsNullOrEmpty(toAppend))
            {
                if (instructions is null)
                {
                    instructions = new(toAppend);
                }
                else
                {
                    _ = instructions.AppendLine().AppendLine(toAppend);
                }
            }
        }

        AppendSystemInstructions(runOptions.AdditionalInstructions);
        AppendSystemInstructions(options?.Instructions);

        // Process ChatMessages.
        List<FunctionResultContent>? functionResults = null;
        foreach (var chatMessage in messages)
        {
            List<MessageContent> messageContents = [];

            // Assistants doesn't support system/developer messages directly. It does support transient per-request instructions,
            // so we can use the system/developer messages to build up a set of instructions that will be passed to the assistant
            // as part of this request. However, in doing so, on a subsequent request that information will be lost, as there's no
            // way to store per-thread instructions in the OpenAI Assistants API. We don't want to convert these to user messages,
            // however, as that would then expose the system/developer messages in a way that might make the model more likely
            // to include that information in its responses. System messages should ideally be instead done as instructions to
            // the assistant when the assistant is created.
            if (chatMessage.Role == ChatRole.System ||
                chatMessage.Role == OpenAIClientExtensions.ChatRoleDeveloper)
            {
                foreach (var textContent in chatMessage.Contents.OfType<TextContent>())
                {
                    AppendSystemInstructions(textContent.Text);
                }

                continue;
            }

            foreach (AIContent content in chatMessage.Contents)
            {
                switch (content)
                {
                    case AIContent when content.RawRepresentation is MessageContent rawRep:
                        messageContents.Add(rawRep);
                        break;

                    case TextContent text:
                        messageContents.Add(MessageContent.FromText(text.Text));
                        break;

                    case UriContent image when image.HasTopLevelMediaType("image"):
                        messageContents.Add(MessageContent.FromImageUri(image.Uri));
                        break;

                    case FunctionResultContent result when chatMessage.Role == ChatRole.Tool:
                        (functionResults ??= []).Add(result);
                        break;
                }
            }

            if (messageContents.Count > 0)
            {
                runOptions.AdditionalMessages.Add(new ThreadInitializationMessage(
                    chatMessage.Role == ChatRole.Assistant ? MessageRole.Assistant : MessageRole.User,
                    messageContents));
            }
        }

        runOptions.AdditionalInstructions = instructions?.ToString();

        return (runOptions, resources, functionResults);
    }

    /// <summary>Convert <see cref="FunctionResultContent"/> instances to <see cref="ToolOutput"/> instances.</summary>
    /// <param name="toolResults">The tool results to process.</param>
    /// <param name="toolOutputs">The generated list of tool outputs, if any could be created.</param>
    /// <returns>The run ID associated with the corresponding function call requests.</returns>
    private static string? ConvertFunctionResultsToToolOutput(List<FunctionResultContent>? toolResults, out List<ToolOutput>? toolOutputs)
    {
        string? runId = null;
        toolOutputs = null;
        if (toolResults?.Count > 0)
        {
            foreach (var frc in toolResults)
            {
                // When creating the FunctionCallContext, we created it with a CallId == [runId, callId].
                // We need to extract the run ID and ensure that the ToolOutput we send back to Azure
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

    /// <summary>
    /// Provides the same behavior as <see cref="EqualityComparer{ToolDefinition}.Default"/>, except
    /// for <see cref="FunctionToolDefinition"/> it compares names so that two function tool definitions with the
    /// same name compare equally.
    /// </summary>
    private sealed class ToolDefinitionNameEqualityComparer : IEqualityComparer<ToolDefinition>
    {
        public static ToolDefinitionNameEqualityComparer Instance { get; } = new();

        public bool Equals(ToolDefinition? x, ToolDefinition? y) =>
            x is FunctionToolDefinition xFtd && y is FunctionToolDefinition yFtd ? xFtd.FunctionName.Equals(yFtd.FunctionName, StringComparison.Ordinal) :
            EqualityComparer<ToolDefinition?>.Default.Equals(x, y);

        public int GetHashCode(ToolDefinition obj) =>
            obj is FunctionToolDefinition ftd ? ftd.FunctionName.GetHashCode(StringComparison.Ordinal) :
            EqualityComparer<ToolDefinition>.Default.GetHashCode(obj);
    }
}

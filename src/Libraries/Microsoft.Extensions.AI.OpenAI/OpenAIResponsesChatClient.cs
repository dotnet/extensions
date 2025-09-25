// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Responses;

#pragma warning disable S907 // "goto" statement should not be used
#pragma warning disable S1067 // Expressions should not be too complex
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable S3254 // Default parameter values should not be passed as arguments
#pragma warning disable S3604 // Member initializer values should not be redundant
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an <see cref="OpenAIResponseClient"/>.</summary>
internal sealed class OpenAIResponsesChatClient : IChatClient
{
    // Fix this to not use reflection once https://github.com/openai/openai-dotnet/issues/643 is addressed.
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
    private static readonly Type? _internalResponseReasoningSummaryTextDeltaEventType = Type.GetType("OpenAI.Responses.InternalResponseReasoningSummaryTextDeltaEvent, OpenAI");
    private static readonly PropertyInfo? _summaryTextDeltaProperty = _internalResponseReasoningSummaryTextDeltaEventType?.GetProperty("Delta");

    // These delegate instances are used to call the internal overloads of CreateResponseAsync and CreateResponseStreamingAsync that accept
    // a RequestOptions. These should be replaced once a better way to pass RequestOptions is available.
    private static readonly Func<OpenAIResponseClient, IEnumerable<ResponseItem>, ResponseCreationOptions, RequestOptions, Task<ClientResult<OpenAIResponse>>>?
        _createResponseAsync =
        (Func<OpenAIResponseClient, IEnumerable<ResponseItem>, ResponseCreationOptions, RequestOptions, Task<ClientResult<OpenAIResponse>>>?)
        typeof(OpenAIResponseClient).GetMethod(
            nameof(OpenAIResponseClient.CreateResponseAsync), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(IEnumerable<ResponseItem>), typeof(ResponseCreationOptions), typeof(RequestOptions)], null)
        ?.CreateDelegate(typeof(Func<OpenAIResponseClient, IEnumerable<ResponseItem>, ResponseCreationOptions, RequestOptions, Task<ClientResult<OpenAIResponse>>>));
    private static readonly Func<OpenAIResponseClient, IEnumerable<ResponseItem>, ResponseCreationOptions, RequestOptions, AsyncCollectionResult<StreamingResponseUpdate>>?
        _createResponseStreamingAsync =
        (Func<OpenAIResponseClient, IEnumerable<ResponseItem>, ResponseCreationOptions, RequestOptions, AsyncCollectionResult<StreamingResponseUpdate>>?)
        typeof(OpenAIResponseClient).GetMethod(
            nameof(OpenAIResponseClient.CreateResponseStreamingAsync), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(IEnumerable<ResponseItem>), typeof(ResponseCreationOptions), typeof(RequestOptions)], null)
        ?.CreateDelegate(typeof(Func<OpenAIResponseClient, IEnumerable<ResponseItem>, ResponseCreationOptions, RequestOptions, AsyncCollectionResult<StreamingResponseUpdate>>));

    /// <summary>Metadata about the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The underlying <see cref="OpenAIResponseClient" />.</summary>
    private readonly OpenAIResponseClient _responseClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAIResponsesChatClient"/> class for the specified <see cref="OpenAIResponseClient"/>.</summary>
    /// <param name="responseClient">The underlying client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="responseClient"/> is <see langword="null"/>.</exception>
    public OpenAIResponsesChatClient(OpenAIResponseClient responseClient)
    {
        _ = Throw.IfNull(responseClient);

        _responseClient = responseClient;

        _metadata = new("openai", responseClient.Endpoint, responseClient.Model);
    }

    /// <inheritdoc />
    object? IChatClient.GetService(Type serviceType, object? serviceKey)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(ChatClientMetadata) ? _metadata :
            serviceType == typeof(OpenAIResponseClient) ? _responseClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var inputMessages = Throw.IfNull(messages) as IReadOnlyCollection<ChatMessage> ?? [.. messages];

        var openAIOptions = ToOpenAIResponseCreationOptions(options);

        // Convert the inputs into what OpenAIResponseClient expects.
        var openAIResponseItems = ToOpenAIResponseItems(inputMessages, options);

        // Provided continuation token signals that an existing background response should be fetched.
        if (options?.ContinuationToken is { } token)
        {
            if (inputMessages.Count > 0)
            {
                throw new InvalidOperationException("Messages are not allowed when continuing a background response using a continuation token.");
            }

            var continuationToken = OpenAIResponsesContinuationToken.FromToken(token);

            var response = await _responseClient.GetResponseAsync(continuationToken.ResponseId, cancellationToken).ConfigureAwait(false);

            return FromOpenAIResponse(response, openAIOptions);
        }

        // Make the call to the OpenAIResponseClient.
        var task = _createResponseAsync is not null ?
            _createResponseAsync(_responseClient, openAIResponseItems, openAIOptions, cancellationToken.ToRequestOptions(streaming: false)) :
            _responseClient.CreateResponseAsync(openAIResponseItems, openAIOptions, cancellationToken);
        var openAIResponse = (await task.ConfigureAwait(false)).Value;

        // Convert the response to a ChatResponse.
        return FromOpenAIResponse(openAIResponse, openAIOptions);
    }

    internal static ChatResponse FromOpenAIResponse(OpenAIResponse openAIResponse, ResponseCreationOptions? openAIOptions)
    {
        // Convert and return the results.
        ChatResponse response = new()
        {
            ConversationId = openAIOptions?.StoredOutputEnabled is false ? null : openAIResponse.Id,
            CreatedAt = openAIResponse.CreatedAt,
            ContinuationToken = GetContinuationToken(openAIResponse),
            FinishReason = ToFinishReason(openAIResponse.IncompleteStatusDetails?.Reason),
            ModelId = openAIResponse.Model,
            RawRepresentation = openAIResponse,
            ResponseId = openAIResponse.Id,
            Usage = ToUsageDetails(openAIResponse),
        };

        if (!string.IsNullOrEmpty(openAIResponse.EndUserId))
        {
            (response.AdditionalProperties ??= [])[nameof(openAIResponse.EndUserId)] = openAIResponse.EndUserId;
        }

        if (openAIResponse.Error is not null)
        {
            (response.AdditionalProperties ??= [])[nameof(openAIResponse.Error)] = openAIResponse.Error;
        }

        if (openAIResponse.OutputItems is not null)
        {
            response.Messages = [.. ToChatMessages(openAIResponse.OutputItems)];

            if (response.Messages.LastOrDefault() is { } lastMessage && openAIResponse.Error is { } error)
            {
                lastMessage.Contents.Add(new ErrorContent(error.Message) { ErrorCode = error.Code.ToString() });
            }

            foreach (var message in response.Messages)
            {
                message.CreatedAt ??= openAIResponse.CreatedAt;
            }
        }

        return response;
    }

    internal static IEnumerable<ChatMessage> ToChatMessages(IEnumerable<ResponseItem> items)
    {
        ChatMessage? message = null;

        foreach (ResponseItem outputItem in items)
        {
            message ??= new(ChatRole.Assistant, (string?)null);

            switch (outputItem)
            {
                case MessageResponseItem messageItem:
                    if (message.MessageId is not null && message.MessageId != messageItem.Id)
                    {
                        yield return message;
                        message = new ChatMessage();
                    }

                    message.MessageId = messageItem.Id;
                    message.RawRepresentation = messageItem;
                    message.Role = ToChatRole(messageItem.Role);
                    ((List<AIContent>)message.Contents).AddRange(ToAIContents(messageItem.Content));
                    break;

                case ReasoningResponseItem reasoningItem:
                    message.Contents.Add(new TextReasoningContent(reasoningItem.GetSummaryText())
                    {
                        ProtectedData = reasoningItem.EncryptedContent,
                        RawRepresentation = outputItem,
                    });
                    break;

                case FunctionCallResponseItem functionCall:
                    var fcc = OpenAIClientExtensions.ParseCallContent(functionCall.FunctionArguments, functionCall.CallId, functionCall.FunctionName);
                    fcc.RawRepresentation = outputItem;
                    message.Contents.Add(fcc);
                    break;

                case McpToolCallItem mtci:
                    AddMcpToolCallContent(mtci, message.Contents);
                    break;

                case McpToolCallApprovalRequestItem mtcari:
                    message.Contents.Add(new McpServerToolApprovalRequestContent(mtcari.Id, new(mtcari.Id, mtcari.ToolName, mtcari.ServerLabel)
                    {
                        Arguments = JsonSerializer.Deserialize(mtcari.ToolArguments.ToMemory().Span, OpenAIJsonContext.Default.IReadOnlyDictionaryStringObject)!,
                        RawRepresentation = mtcari,
                    })
                    {
                        RawRepresentation = mtcari,
                    });
                    break;

                case McpToolCallApprovalResponseItem mtcari:
                    message.Contents.Add(new McpServerToolApprovalResponseContent(mtcari.ApprovalRequestId, mtcari.Approved));
                    break;

                case FunctionCallOutputResponseItem functionCallOutputItem:
                    message.Contents.Add(new FunctionResultContent(functionCallOutputItem.CallId, functionCallOutputItem.FunctionOutput) { RawRepresentation = functionCallOutputItem });
                    break;

                default:
                    message.Contents.Add(new() { RawRepresentation = outputItem });
                    break;
            }
        }

        if (message is not null)
        {
            yield return message;
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        var inputMessages = Throw.IfNull(messages) as IReadOnlyCollection<ChatMessage> ?? [.. messages];

        var openAIOptions = ToOpenAIResponseCreationOptions(options);

        // Provided continuation token signals that an existing background response should be fetched.
        if (options?.ContinuationToken is { } token)
        {
            if (inputMessages.Count > 0)
            {
                throw new InvalidOperationException("Messages are not allowed when resuming steamed background response using a continuation token.");
            }

            var continuationToken = OpenAIResponsesContinuationToken.FromToken(token);

            IAsyncEnumerable<StreamingResponseUpdate> updates = _responseClient.GetResponseStreamingAsync(continuationToken.ResponseId, continuationToken.SequenceNumber, cancellationToken);

            return FromOpenAIStreamingResponseUpdatesAsync(updates, openAIOptions, continuationToken.ResponseId, cancellationToken);
        }

        var openAIResponseItems = ToOpenAIResponseItems(inputMessages, options);

        var streamingUpdates = _createResponseStreamingAsync is not null ?
            _createResponseStreamingAsync(_responseClient, openAIResponseItems, openAIOptions, cancellationToken.ToRequestOptions(streaming: true)) :
            _responseClient.CreateResponseStreamingAsync(openAIResponseItems, openAIOptions, cancellationToken);

        return FromOpenAIStreamingResponseUpdatesAsync(streamingUpdates, openAIOptions, cancellationToken: cancellationToken);
    }

    internal static async IAsyncEnumerable<ChatResponseUpdate> FromOpenAIStreamingResponseUpdatesAsync(
        IAsyncEnumerable<StreamingResponseUpdate> streamingResponseUpdates,
        ResponseCreationOptions? options,
        string? resumeResponseId = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        DateTimeOffset? createdAt = null;
        string? responseId = resumeResponseId;
        string? conversationId = options?.StoredOutputEnabled is false ? null : resumeResponseId;
        string? modelId = null;
        string? lastMessageId = null;
        ChatRole? lastRole = null;
        bool anyFunctions = false;
        ResponseStatus? latestResponseStatus = null;

        await foreach (var streamingUpdate in streamingResponseUpdates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Create an update populated with the current state of the response.
            ChatResponseUpdate CreateUpdate(AIContent? content = null) =>
                new(lastRole, content is not null ? [content] : null)
                {
                    ConversationId = conversationId,
                    CreatedAt = createdAt,
                    MessageId = lastMessageId,
                    ModelId = modelId,
                    RawRepresentation = streamingUpdate,
                    ResponseId = responseId,
                    ContinuationToken = GetContinuationToken(
                        responseId!,
                        latestResponseStatus,
                        options?.BackgroundModeEnabled,
                        streamingUpdate.SequenceNumber)
                };

            switch (streamingUpdate)
            {
                case StreamingResponseCreatedUpdate createdUpdate:
                    createdAt = createdUpdate.Response.CreatedAt;
                    responseId = createdUpdate.Response.Id;
                    conversationId = options?.StoredOutputEnabled is false ? null : responseId;
                    modelId = createdUpdate.Response.Model;
                    latestResponseStatus = createdUpdate.Response.Status;
                    goto default;

                case StreamingResponseQueuedUpdate queuedUpdate:
                    createdAt = queuedUpdate.Response.CreatedAt;
                    responseId = queuedUpdate.Response.Id;
                    conversationId = options?.StoredOutputEnabled is false ? null : responseId;
                    modelId = queuedUpdate.Response.Model;
                    latestResponseStatus = queuedUpdate.Response.Status;
                    goto default;

                case StreamingResponseInProgressUpdate inProgressUpdate:
                    createdAt = inProgressUpdate.Response.CreatedAt;
                    responseId = inProgressUpdate.Response.Id;
                    conversationId = options?.StoredOutputEnabled is false ? null : responseId;
                    modelId = inProgressUpdate.Response.Model;
                    latestResponseStatus = inProgressUpdate.Response.Status;
                    goto default;

                case StreamingResponseIncompleteUpdate incompleteUpdate:
                    createdAt = incompleteUpdate.Response.CreatedAt;
                    responseId = incompleteUpdate.Response.Id;
                    conversationId = options?.StoredOutputEnabled is false ? null : responseId;
                    modelId = incompleteUpdate.Response.Model;
                    latestResponseStatus = incompleteUpdate.Response.Status;
                    goto default;

                case StreamingResponseFailedUpdate failedUpdate:
                    createdAt = failedUpdate.Response.CreatedAt;
                    responseId = failedUpdate.Response.Id;
                    conversationId = options?.StoredOutputEnabled is false ? null : responseId;
                    modelId = failedUpdate.Response.Model;
                    latestResponseStatus = failedUpdate.Response.Status;
                    goto default;

                case StreamingResponseCompletedUpdate completedUpdate:
                {
                    createdAt = completedUpdate.Response.CreatedAt;
                    responseId = completedUpdate.Response.Id;
                    conversationId = options?.StoredOutputEnabled is false ? null : responseId;
                    modelId = completedUpdate.Response.Model;
                    latestResponseStatus = completedUpdate.Response?.Status;
                    var update = CreateUpdate(ToUsageDetails(completedUpdate.Response) is { } usage ? new UsageContent(usage) : null);
                    update.FinishReason =
                        ToFinishReason(completedUpdate.Response?.IncompleteStatusDetails?.Reason) ??
                        (anyFunctions ? ChatFinishReason.ToolCalls :
                        ChatFinishReason.Stop);
                    yield return update;
                    break;
                }

                case StreamingResponseOutputItemAddedUpdate outputItemAddedUpdate:
                    switch (outputItemAddedUpdate.Item)
                    {
                        case MessageResponseItem mri:
                            lastMessageId = outputItemAddedUpdate.Item.Id;
                            lastRole = ToChatRole(mri.Role);
                            break;

                        case FunctionCallResponseItem fcri:
                            anyFunctions = true;
                            lastRole = ChatRole.Assistant;
                            break;
                    }

                    goto default;

                case StreamingResponseOutputTextDeltaUpdate outputTextDeltaUpdate:
                    yield return CreateUpdate(new TextContent(outputTextDeltaUpdate.Delta));
                    break;

                case StreamingResponseOutputItemDoneUpdate outputItemDoneUpdate when outputItemDoneUpdate.Item is FunctionCallResponseItem fcri:
                    yield return CreateUpdate(OpenAIClientExtensions.ParseCallContent(fcri.FunctionArguments.ToString(), fcri.CallId, fcri.FunctionName));
                    break;

                case StreamingResponseOutputItemDoneUpdate outputItemDoneUpdate when outputItemDoneUpdate.Item is McpToolCallItem mtci:
                    var mcpUpdate = CreateUpdate();
                    AddMcpToolCallContent(mtci, mcpUpdate.Contents);
                    yield return mcpUpdate;
                    break;

                case StreamingResponseOutputItemDoneUpdate outputItemDoneUpdate when outputItemDoneUpdate.Item is McpToolDefinitionListItem mtdli:
                    yield return CreateUpdate(new AIContent { RawRepresentation = mtdli });
                    break;

                case StreamingResponseOutputItemDoneUpdate outputItemDoneUpdate when outputItemDoneUpdate.Item is McpToolCallApprovalRequestItem mtcari:
                    yield return CreateUpdate(new McpServerToolApprovalRequestContent(mtcari.Id, new(mtcari.Id, mtcari.ToolName, mtcari.ServerLabel)
                    {
                        Arguments = JsonSerializer.Deserialize(mtcari.ToolArguments.ToMemory().Span, OpenAIJsonContext.Default.IReadOnlyDictionaryStringObject)!,
                        RawRepresentation = mtcari,
                    })
                    {
                        RawRepresentation = mtcari,
                    });
                    break;

                case StreamingResponseOutputItemDoneUpdate outputItemDoneUpdate when
                        outputItemDoneUpdate.Item is MessageResponseItem mri &&
                        mri.Content is { Count: > 0 } content &&
                        content.Any(c => c.OutputTextAnnotations is { Count: > 0 }):
                    AIContent annotatedContent = new();
                    foreach (var c in content)
                    {
                        PopulateAnnotations(c, annotatedContent);
                    }

                    yield return CreateUpdate(annotatedContent);
                    break;

                case StreamingResponseErrorUpdate errorUpdate:
                    yield return CreateUpdate(new ErrorContent(errorUpdate.Message)
                    {
                        ErrorCode = errorUpdate.Code,
                        Details = errorUpdate.Param,
                    });
                    break;

                case StreamingResponseRefusalDoneUpdate refusalDone:
                    yield return CreateUpdate(new ErrorContent(refusalDone.Refusal)
                    {
                        ErrorCode = nameof(ResponseContentPart.Refusal),
                    });
                    break;

                // Replace with public StreamingResponseReasoningSummaryTextDelta when available
                case StreamingResponseUpdate when
                        streamingUpdate.GetType() == _internalResponseReasoningSummaryTextDeltaEventType &&
                        _summaryTextDeltaProperty?.GetValue(streamingUpdate) is string delta:
                    yield return CreateUpdate(new TextReasoningContent(delta));
                    break;

                default:
                    yield return CreateUpdate();
                    break;
            }
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IChatClient interface.
    }

    internal static FunctionTool ToResponseTool(AIFunctionDeclaration aiFunction, ChatOptions? options = null)
    {
        bool? strict =
            OpenAIClientExtensions.HasStrict(aiFunction.AdditionalProperties) ??
            OpenAIClientExtensions.HasStrict(options?.AdditionalProperties);

        return ResponseTool.CreateFunctionTool(
            aiFunction.Name,
            OpenAIClientExtensions.ToOpenAIFunctionParameters(aiFunction, strict),
            strict,
            aiFunction.Description);
    }

    /// <summary>Creates a <see cref="ChatRole"/> from a <see cref="MessageRole"/>.</summary>
    private static ChatRole ToChatRole(MessageRole? role) =>
        role switch
        {
            MessageRole.System => ChatRole.System,
            MessageRole.Developer => OpenAIClientExtensions.ChatRoleDeveloper,
            MessageRole.User => ChatRole.User,
            _ => ChatRole.Assistant,
        };

    /// <summary>Creates a <see cref="ChatFinishReason"/> from a <see cref="ResponseIncompleteStatusReason"/>.</summary>
    private static ChatFinishReason? ToFinishReason(ResponseIncompleteStatusReason? statusReason) =>
        statusReason == ResponseIncompleteStatusReason.ContentFilter ? ChatFinishReason.ContentFilter :
        statusReason == ResponseIncompleteStatusReason.MaxOutputTokens ? ChatFinishReason.Length :
        null;

    /// <summary>Converts a <see cref="ChatOptions"/> to a <see cref="ResponseCreationOptions"/>.</summary>
    private ResponseCreationOptions ToOpenAIResponseCreationOptions(ChatOptions? options)
    {
        if (options is null)
        {
            return new ResponseCreationOptions();
        }

        if (options.RawRepresentationFactory?.Invoke(this) is not ResponseCreationOptions result)
        {
            result = new ResponseCreationOptions();
        }

        // Handle strongly-typed properties.
        result.MaxOutputTokenCount ??= options.MaxOutputTokens;
        result.PreviousResponseId ??= options.ConversationId;
        result.Temperature ??= options.Temperature;
        result.TopP ??= options.TopP;
        result.BackgroundModeEnabled ??= options.BackgroundResponsesOptions?.Allow;

        if (options.Instructions is { } instructions)
        {
            result.Instructions = string.IsNullOrEmpty(result.Instructions) ?
                instructions :
                $"{result.Instructions}{Environment.NewLine}{instructions}";
        }

        // Populate tools if there are any.
        if (options.Tools is { Count: > 0 } tools)
        {
            foreach (AITool tool in tools)
            {
                switch (tool)
                {
                    case ResponseToolAITool rtat:
                        result.Tools.Add(rtat.Tool);
                        break;

                    case AIFunctionDeclaration aiFunction:
                        result.Tools.Add(ToResponseTool(aiFunction, options));
                        break;

                    case HostedWebSearchTool webSearchTool:
                        WebSearchToolLocation? location = null;
                        if (webSearchTool.AdditionalProperties.TryGetValue(nameof(WebSearchToolLocation), out object? objLocation))
                        {
                            location = objLocation as WebSearchToolLocation;
                        }

                        WebSearchToolContextSize? size = null;
                        if (webSearchTool.AdditionalProperties.TryGetValue(nameof(WebSearchToolContextSize), out object? objSize) &&
                            objSize is WebSearchToolContextSize)
                        {
                            size = (WebSearchToolContextSize)objSize;
                        }

                        result.Tools.Add(ResponseTool.CreateWebSearchTool(location, size));
                        break;

                    case HostedFileSearchTool fileSearchTool:
                        result.Tools.Add(ResponseTool.CreateFileSearchTool(
                            fileSearchTool.Inputs?.OfType<HostedVectorStoreContent>().Select(c => c.VectorStoreId) ?? [],
                            fileSearchTool.MaximumResultCount));
                        break;

                    case HostedCodeInterpreterTool codeTool:
                        result.Tools.Add(
                            ResponseTool.CreateCodeInterpreterTool(
                                new CodeInterpreterToolContainer(codeTool.Inputs?.OfType<HostedFileContent>().Select(f => f.FileId).ToList() is { Count: > 0 } ids ?
                                    CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration(ids) :
                                    new())));
                        break;

                    case HostedMcpServerTool mcpTool:
                        McpTool responsesMcpTool = ResponseTool.CreateMcpTool(
                            mcpTool.ServerName,
                            mcpTool.Url,
                            serverDescription: mcpTool.ServerDescription,
                            headers: mcpTool.Headers);

                        if (mcpTool.AllowedTools is not null)
                        {
                            responsesMcpTool.AllowedTools = new();
                            AddAllMcpFilters(mcpTool.AllowedTools, responsesMcpTool.AllowedTools);
                        }

                        switch (mcpTool.ApprovalMode)
                        {
                            case HostedMcpServerToolAlwaysRequireApprovalMode:
                                responsesMcpTool.ToolCallApprovalPolicy = new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.AlwaysRequireApproval);
                                break;

                            case HostedMcpServerToolNeverRequireApprovalMode:
                                responsesMcpTool.ToolCallApprovalPolicy = new McpToolCallApprovalPolicy(GlobalMcpToolCallApprovalPolicy.NeverRequireApproval);
                                break;

                            case HostedMcpServerToolRequireSpecificApprovalMode specificMode:
                                responsesMcpTool.ToolCallApprovalPolicy = new McpToolCallApprovalPolicy(new CustomMcpToolCallApprovalPolicy());

                                if (specificMode.AlwaysRequireApprovalToolNames is { Count: > 0 } alwaysRequireToolNames)
                                {
                                    responsesMcpTool.ToolCallApprovalPolicy.CustomPolicy.ToolsAlwaysRequiringApproval = new();
                                    AddAllMcpFilters(alwaysRequireToolNames, responsesMcpTool.ToolCallApprovalPolicy.CustomPolicy.ToolsAlwaysRequiringApproval);
                                }

                                if (specificMode.NeverRequireApprovalToolNames is { Count: > 0 } neverRequireToolNames)
                                {
                                    responsesMcpTool.ToolCallApprovalPolicy.CustomPolicy.ToolsNeverRequiringApproval = new();
                                    AddAllMcpFilters(neverRequireToolNames, responsesMcpTool.ToolCallApprovalPolicy.CustomPolicy.ToolsNeverRequiringApproval);
                                }

                                break;
                        }

                        result.Tools.Add(responsesMcpTool);
                        break;
                }
            }

            if (result.Tools.Count > 0)
            {
                result.ParallelToolCallsEnabled ??= options.AllowMultipleToolCalls;
            }

            if (result.ToolChoice is null && result.Tools.Count > 0)
            {
                switch (options.ToolMode)
                {
                    case NoneChatToolMode:
                        result.ToolChoice = ResponseToolChoice.CreateNoneChoice();
                        break;

                    case AutoChatToolMode:
                    case null:
                        result.ToolChoice = ResponseToolChoice.CreateAutoChoice();
                        break;

                    case RequiredChatToolMode required:
                        result.ToolChoice = required.RequiredFunctionName is not null ?
                            ResponseToolChoice.CreateFunctionChoice(required.RequiredFunctionName) :
                            ResponseToolChoice.CreateRequiredChoice();
                        break;
                }
            }
        }

        if (result.TextOptions?.TextFormat is null &&
            ToOpenAIResponseTextFormat(options.ResponseFormat, options) is { } newFormat)
        {
            (result.TextOptions ??= new()).TextFormat = newFormat;
        }

        return result;
    }

    internal static ResponseTextFormat? ToOpenAIResponseTextFormat(ChatResponseFormat? format, ChatOptions? options = null) =>
        format switch
        {
            ChatResponseFormatText => ResponseTextFormat.CreateTextFormat(),

            ChatResponseFormatJson jsonFormat when OpenAIClientExtensions.StrictSchemaTransformCache.GetOrCreateTransformedSchema(jsonFormat) is { } jsonSchema =>
                ResponseTextFormat.CreateJsonSchemaFormat(
                    jsonFormat.SchemaName ?? "json_schema",
                    BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(jsonSchema, OpenAIJsonContext.Default.JsonElement)),
                    jsonFormat.SchemaDescription,
                    OpenAIClientExtensions.HasStrict(options?.AdditionalProperties)),

            ChatResponseFormatJson => ResponseTextFormat.CreateJsonObjectFormat(),

            _ => null,
        };

    /// <summary>Convert a sequence of <see cref="ChatMessage"/>s to <see cref="ResponseItem"/>s.</summary>
    internal static IEnumerable<ResponseItem> ToOpenAIResponseItems(IEnumerable<ChatMessage> inputs, ChatOptions? options)
    {
        _ = options; // currently unused

        Dictionary<string, AIContent>? idToContentMapping = null;

        foreach (ChatMessage input in inputs)
        {
            if (input.Role == ChatRole.System ||
                input.Role == OpenAIClientExtensions.ChatRoleDeveloper)
            {
                string text = input.Text;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    yield return input.Role == ChatRole.System ?
                        ResponseItem.CreateSystemMessageItem(text) :
                        ResponseItem.CreateDeveloperMessageItem(text);
                }

                continue;
            }

            if (input.Role == ChatRole.User)
            {
                yield return ResponseItem.CreateUserMessageItem(ToResponseContentParts(input.Contents));
                continue;
            }

            if (input.Role == ChatRole.Tool)
            {
                foreach (AIContent item in input.Contents)
                {
                    switch (item)
                    {
                        case AIContent when item.RawRepresentation is ResponseItem rawRep:
                            yield return rawRep;
                            break;

                        case FunctionResultContent resultContent:
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

                            yield return ResponseItem.CreateFunctionCallOutputItem(resultContent.CallId, result ?? string.Empty);
                            break;

                        case McpServerToolApprovalResponseContent mcpApprovalResponseContent:
                            yield return ResponseItem.CreateMcpApprovalResponseItem(mcpApprovalResponseContent.Id, mcpApprovalResponseContent.Approved);
                            break;
                    }
                }

                continue;
            }

            if (input.Role == ChatRole.Assistant)
            {
                foreach (AIContent item in input.Contents)
                {
                    switch (item)
                    {
                        case AIContent when item.RawRepresentation is ResponseItem rawRep:
                            yield return rawRep;
                            break;

                        case TextContent textContent:
                            yield return ResponseItem.CreateAssistantMessageItem(textContent.Text);
                            break;

                        case TextReasoningContent reasoningContent:
                            yield return OpenAIResponsesModelFactory.ReasoningResponseItem(
                                encryptedContent: reasoningContent.ProtectedData,
                                summaryText: reasoningContent.Text);
                            break;

                        case FunctionCallContent callContent:
                            yield return ResponseItem.CreateFunctionCallItem(
                                callContent.CallId,
                                callContent.Name,
                                BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(
                                    callContent.Arguments,
                                    AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(IDictionary<string, object?>)))));
                            break;

                        case McpServerToolApprovalRequestContent mcpApprovalRequestContent:
                            yield return ResponseItem.CreateMcpApprovalRequestItem(
                                mcpApprovalRequestContent.Id,
                                mcpApprovalRequestContent.ToolCall.ServerName,
                                mcpApprovalRequestContent.ToolCall.ToolName,
                                BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(mcpApprovalRequestContent.ToolCall.Arguments!, OpenAIJsonContext.Default.IReadOnlyDictionaryStringObject)));
                            break;

                        case McpServerToolCallContent mstcc:
                            (idToContentMapping ??= [])[mstcc.CallId] = mstcc;
                            break;

                        case McpServerToolResultContent mstrc:
                            if (idToContentMapping?.TryGetValue(mstrc.CallId, out AIContent? callContentFromMapping) is true &&
                                callContentFromMapping is McpServerToolCallContent associatedCall)
                            {
                                _ = idToContentMapping.Remove(mstrc.CallId);
                                McpToolCallItem mtci = ResponseItem.CreateMcpToolCallItem(
                                    associatedCall.ServerName,
                                    associatedCall.ToolName,
                                    BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(associatedCall.Arguments!, OpenAIJsonContext.Default.IReadOnlyDictionaryStringObject)));
                                if (mstrc.Output?.OfType<ErrorContent>().FirstOrDefault() is ErrorContent errorContent)
                                {
                                    mtci.Error = BinaryData.FromString(errorContent.Message);
                                }
                                else
                                {
                                    mtci.ToolOutput = string.Concat(mstrc.Output?.OfType<TextContent>() ?? []);
                                }

                                yield return mtci;
                            }

                            break;
                    }
                }

                continue;
            }
        }
    }

    /// <summary>Extract usage details from an <see cref="OpenAIResponse"/>.</summary>
    private static UsageDetails? ToUsageDetails(OpenAIResponse? openAIResponse)
    {
        UsageDetails? ud = null;
        if (openAIResponse?.Usage is { } usage)
        {
            ud = new()
            {
                InputTokenCount = usage.InputTokenCount,
                OutputTokenCount = usage.OutputTokenCount,
                TotalTokenCount = usage.TotalTokenCount,
            };

            if (usage.InputTokenDetails is { } inputDetails)
            {
                ud.AdditionalCounts ??= [];
                ud.AdditionalCounts.Add($"{nameof(usage.InputTokenDetails)}.{nameof(inputDetails.CachedTokenCount)}", inputDetails.CachedTokenCount);
            }

            if (usage.OutputTokenDetails is { } outputDetails)
            {
                ud.AdditionalCounts ??= [];
                ud.AdditionalCounts.Add($"{nameof(usage.OutputTokenDetails)}.{nameof(outputDetails.ReasoningTokenCount)}", outputDetails.ReasoningTokenCount);
            }
        }

        return ud;
    }

    /// <summary>Convert a sequence of <see cref="ResponseContentPart"/>s to a list of <see cref="AIContent"/>.</summary>
    private static List<AIContent> ToAIContents(IEnumerable<ResponseContentPart> contents)
    {
        List<AIContent> results = [];

        foreach (ResponseContentPart part in contents)
        {
            switch (part.Kind)
            {
                case ResponseContentPartKind.InputText or ResponseContentPartKind.OutputText:
                    TextContent text = new(part.Text) { RawRepresentation = part };
                    PopulateAnnotations(part, text);
                    results.Add(text);
                    break;

                case ResponseContentPartKind.InputFile:
                    if (!string.IsNullOrWhiteSpace(part.InputImageFileId))
                    {
                        results.Add(new HostedFileContent(part.InputImageFileId) { RawRepresentation = part });
                    }
                    else if (!string.IsNullOrWhiteSpace(part.InputFileId))
                    {
                        results.Add(new HostedFileContent(part.InputFileId) { RawRepresentation = part });
                    }
                    else if (part.InputFileBytes is not null)
                    {
                        results.Add(new DataContent(part.InputFileBytes, part.InputFileBytesMediaType ?? "application/octet-stream")
                        {
                            Name = part.InputFilename,
                            RawRepresentation = part,
                        });
                    }

                    break;

                case ResponseContentPartKind.Refusal:
                    results.Add(new ErrorContent(part.Refusal)
                    {
                        ErrorCode = nameof(ResponseContentPartKind.Refusal),
                        RawRepresentation = part,
                    });
                    break;

                default:
                    results.Add(new() { RawRepresentation = part });
                    break;
            }
        }

        return results;
    }

    /// <summary>Converts any annotations from <paramref name="source"/> and stores them in <paramref name="destination"/>.</summary>
    private static void PopulateAnnotations(ResponseContentPart source, AIContent destination)
    {
        if (source.OutputTextAnnotations is { Count: > 0 })
        {
            foreach (var ota in source.OutputTextAnnotations)
            {
                CitationAnnotation ca = new()
                {
                    RawRepresentation = ota,
                };

                switch (ota)
                {
                    case UriCitationMessageAnnotation ucma:
                        ca.AnnotatedRegions = [new TextSpanAnnotatedRegion { StartIndex = ucma.StartIndex, EndIndex = ucma.EndIndex }];
                        ca.Title = ucma.Title;
                        ca.Url = ucma.Uri;
                        break;

                    case FilePathMessageAnnotation fpma:
                        ca.FileId = fpma.FileId;
                        break;

                    case FileCitationMessageAnnotation fcma:
                        ca.FileId = fcma.FileId;
                        break;
                }

                (destination.Annotations ??= []).Add(ca);
            }
        }
    }

    /// <summary>Convert a list of <see cref="AIContent"/>s to a list of <see cref="ResponseContentPart"/>.</summary>
    private static List<ResponseContentPart> ToResponseContentParts(IList<AIContent> contents)
    {
        List<ResponseContentPart> parts = [];
        foreach (var content in contents)
        {
            switch (content)
            {
                case AIContent when content.RawRepresentation is ResponseContentPart rawRep:
                    parts.Add(rawRep);
                    break;

                case TextContent textContent:
                    parts.Add(ResponseContentPart.CreateInputTextPart(textContent.Text));
                    break;

                case UriContent uriContent when uriContent.HasTopLevelMediaType("image"):
                    parts.Add(ResponseContentPart.CreateInputImagePart(uriContent.Uri));
                    break;

                case DataContent dataContent when dataContent.HasTopLevelMediaType("image"):
                    parts.Add(ResponseContentPart.CreateInputImagePart(BinaryData.FromBytes(dataContent.Data), dataContent.MediaType));
                    break;

                case DataContent dataContent when dataContent.MediaType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase):
                    parts.Add(ResponseContentPart.CreateInputFilePart(BinaryData.FromBytes(dataContent.Data), dataContent.MediaType, dataContent.Name ?? $"{Guid.NewGuid():N}.pdf"));
                    break;

                case HostedFileContent fileContent:
                    parts.Add(ResponseContentPart.CreateInputFilePart(fileContent.FileId));
                    break;

                case ErrorContent errorContent when errorContent.ErrorCode == nameof(ResponseContentPartKind.Refusal):
                    parts.Add(ResponseContentPart.CreateRefusalPart(errorContent.Message));
                    break;
            }
        }

        if (parts.Count == 0)
        {
            parts.Add(ResponseContentPart.CreateInputTextPart(string.Empty));
        }

        return parts;
    }

    /// <summary>Adds new <see cref="AIContent"/> for the specified <paramref name="mtci"/> into <paramref name="contents"/>.</summary>
    private static void AddMcpToolCallContent(McpToolCallItem mtci, IList<AIContent> contents)
    {
        contents.Add(new McpServerToolCallContent(mtci.Id, mtci.ToolName, mtci.ServerLabel)
        {
            Arguments = JsonSerializer.Deserialize(mtci.ToolArguments.ToMemory().Span, OpenAIJsonContext.Default.IReadOnlyDictionaryStringObject)!,

            // We purposefully do not set the RawRepresentation on the McpServerToolCallContent, only on the McpServerToolResultContent, to avoid
            // the same McpToolCallItem being included on two different AIContent instances. When these are roundtripped, we want only one
            // McpToolCallItem sent back for the pair.
        });

        contents.Add(new McpServerToolResultContent(mtci.Id)
        {
            RawRepresentation = mtci,
            Output = [mtci.Error is not null ?
                new ErrorContent(mtci.Error.ToString()) :
                new TextContent(mtci.ToolOutput)],
        });
    }

    /// <summary>Adds all of the tool names from <paramref name="toolNames"/> to <paramref name="filter"/>.</summary>
    private static void AddAllMcpFilters(IList<string> toolNames, McpToolFilter filter)
    {
        foreach (var toolName in toolNames)
        {
            filter.ToolNames.Add(toolName);
        }
    }

    private static OpenAIResponsesContinuationToken? GetContinuationToken(OpenAIResponse openAIResponse)
    {
        return GetContinuationToken(
            responseId: openAIResponse.Id,
            responseStatus: openAIResponse.Status,
            isBackgroundModeEnabled: openAIResponse.BackgroundModeEnabled);
    }

    private static OpenAIResponsesContinuationToken? GetContinuationToken(
        string responseId,
        ResponseStatus? responseStatus,
        bool? isBackgroundModeEnabled,
        int? updateSequenceNumber = null)
    {
        if (isBackgroundModeEnabled is not true)
        {
            return null;
        }

        // Return a continuation token for in-progress or queued responses because they are not yet complete.
        if (responseStatus is (ResponseStatus.InProgress or ResponseStatus.Queued))
        {
            return new OpenAIResponsesContinuationToken(responseId)
            {
                SequenceNumber = updateSequenceNumber,
            };
        }
        else if (responseStatus is null && updateSequenceNumber is not null)
        {
            // In some cases, streaming needs to be resumed from an event (e.g., a text delta event) that does not have a response status,
            // response Id, and potentially other properties. In these cases, we know that the response is not yet complete
            // because we are receiving updates for it, so we create a continuation token from the response Id obtained from the continuation token
            // supplied to resume the streaming and the sequence number available on the event.
            return new OpenAIResponsesContinuationToken(responseId)
            {
                SequenceNumber = updateSequenceNumber,
            };
        }

        // For all other statuses: completed, failed, canceled, incomplete
        // return null to indicate the operation is finished allowing the caller
        // to stop and access the final result, failure details, reason for incompletion, etc.
        return null;
    }

    /// <summary>Provides an <see cref="AITool"/> wrapper for a <see cref="ResponseTool"/>.</summary>
    internal sealed class ResponseToolAITool(ResponseTool tool) : AITool
    {
        public ResponseTool Tool => tool;
        public override string Name => Tool.GetType().Name;

        /// <inheritdoc />
        public override object? GetService(Type serviceType, object? serviceKey = null)
        {
            _ = Throw.IfNull(serviceType);

            return
                serviceKey is null && serviceType.IsInstanceOfType(Tool) ? Tool :
                base.GetService(serviceType, serviceKey);
        }
    }
}

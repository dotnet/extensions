// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Responses;

#pragma warning disable S1226 // Method parameters, caught exceptions and foreach variables' initial values should not be ignored
#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable S3254 // Default parameter values should not be passed as arguments
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an <see cref="ResponsesClient"/>.</summary>
internal sealed class OpenAIResponsesChatClient : IChatClient
{
    // These delegate instances are used to call the internal overloads of CreateResponseAsync and CreateResponseStreamingAsync that accept
    // a RequestOptions. These should be replaced once a better way to pass RequestOptions is available.

    private static readonly Func<ResponsesClient, CreateResponseOptions, RequestOptions, AsyncCollectionResult<StreamingResponseUpdate>>?
        _createResponseStreamingAsync =
        (Func<ResponsesClient, CreateResponseOptions, RequestOptions, AsyncCollectionResult<StreamingResponseUpdate>>?)
        typeof(ResponsesClient).GetMethod(
            nameof(ResponsesClient.CreateResponseStreamingAsync), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(CreateResponseOptions), typeof(RequestOptions)], null)
        ?.CreateDelegate(typeof(Func<ResponsesClient, CreateResponseOptions, RequestOptions, AsyncCollectionResult<StreamingResponseUpdate>>));

    private static readonly Func<ResponsesClient, GetResponseOptions, RequestOptions, AsyncCollectionResult<StreamingResponseUpdate>>?
        _getResponseStreamingAsync =
        (Func<ResponsesClient, GetResponseOptions, RequestOptions, AsyncCollectionResult<StreamingResponseUpdate>>?)
        typeof(ResponsesClient).GetMethod(
            nameof(ResponsesClient.GetResponseStreamingAsync), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            null, [typeof(GetResponseOptions), typeof(RequestOptions)], null)
        ?.CreateDelegate(typeof(Func<ResponsesClient, GetResponseOptions, RequestOptions, AsyncCollectionResult<StreamingResponseUpdate>>));

    /// <summary>Metadata about the client.</summary>
    private readonly ChatClientMetadata _metadata;

    /// <summary>The underlying <see cref="ResponsesClient" />.</summary>
    private readonly ResponsesClient _responseClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAIResponsesChatClient"/> class for the specified <see cref="ResponsesClient"/>.</summary>
    /// <param name="responseClient">The underlying client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="responseClient"/> is <see langword="null"/>.</exception>
    public OpenAIResponsesChatClient(ResponsesClient responseClient)
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
            serviceType == typeof(ResponsesClient) ? _responseClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        // Convert the inputs into what ResponsesClient expects.
        var openAIOptions = AsCreateResponseOptions(options, out string? openAIConversationId);

        // Provided continuation token signals that an existing background response should be fetched.
        if (GetContinuationToken(messages, options) is { } token)
        {
            var getTask = _responseClient.GetResponseAsync(token.ResponseId, include: null, stream: null, startingAfter: null, includeObfuscation: null, cancellationToken.ToRequestOptions(streaming: false));
            var response = (ResponseResult)await getTask.ConfigureAwait(false);
            return FromOpenAIResponse(response, openAIOptions, openAIConversationId);
        }

        foreach (var responseItem in ToOpenAIResponseItems(messages, options))
        {
            openAIOptions.InputItems.Add(responseItem);
        }

        // Make the call to the ResponsesClient.
        var createTask = _responseClient.CreateResponseAsync((BinaryContent)openAIOptions, cancellationToken.ToRequestOptions(streaming: false));
        var openAIResponsesResult = (ResponseResult)await createTask.ConfigureAwait(false);

        // Convert the response to a ChatResponse.
        return FromOpenAIResponse(openAIResponsesResult, openAIOptions, openAIConversationId);
    }

    internal static ChatResponse FromOpenAIResponse(ResponseResult responseResult, CreateResponseOptions? openAIOptions, string? conversationId)
    {
        // Convert and return the results.
        ChatResponse response = new()
        {
            ConversationId = openAIOptions?.StoredOutputEnabled is false ? null : (conversationId ?? responseResult.Id),
            CreatedAt = responseResult.CreatedAt,
            ContinuationToken = CreateContinuationToken(responseResult),
            FinishReason = AsFinishReason(responseResult.IncompleteStatusDetails?.Reason),
            ModelId = responseResult.Model,
            RawRepresentation = responseResult,
            ResponseId = responseResult.Id,
            Usage = ToUsageDetails(responseResult),
        };

        if (!string.IsNullOrEmpty(responseResult.EndUserId))
        {
            (response.AdditionalProperties ??= [])[nameof(responseResult.EndUserId)] = responseResult.EndUserId;
        }

        if (responseResult.Error is not null)
        {
            (response.AdditionalProperties ??= [])[nameof(responseResult.Error)] = responseResult.Error;
        }

        if (responseResult.OutputItems is not null)
        {
            response.Messages = [.. ToChatMessages(responseResult.OutputItems, openAIOptions)];

            if (response.Messages.LastOrDefault() is { } lastMessage && responseResult.Error is { } error)
            {
                lastMessage.Contents.Add(new ErrorContent(error.Message) { ErrorCode = error.Code.ToString() });
            }

            foreach (var message in response.Messages)
            {
                message.CreatedAt ??= responseResult.CreatedAt;
            }
        }

        if (responseResult.SafetyIdentifier is not null)
        {
            (response.AdditionalProperties ??= [])[nameof(responseResult.SafetyIdentifier)] = responseResult.SafetyIdentifier;
        }

        return response;
    }

    internal static IEnumerable<ChatMessage> ToChatMessages(IEnumerable<ResponseItem> items, CreateResponseOptions? options = null)
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
                    message.Role = AsChatRole(messageItem.Role);
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

                case FunctionCallOutputResponseItem functionCallOutputItem:
                    message.Contents.Add(new FunctionResultContent(functionCallOutputItem.CallId, functionCallOutputItem.FunctionOutput) { RawRepresentation = functionCallOutputItem });
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
                    message.Contents.Add(new McpServerToolApprovalResponseContent(mtcari.ApprovalRequestId, mtcari.Approved) { RawRepresentation = mtcari });
                    break;

                case CodeInterpreterCallResponseItem cicri:
                    AddCodeInterpreterContents(cicri, message.Contents);
                    break;

                case ImageGenerationCallResponseItem imageGenItem:
                    AddImageGenerationContents(imageGenItem, options, message.Contents);
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
        _ = Throw.IfNull(messages);

        var openAIOptions = AsCreateResponseOptions(options, out string? openAIConversationId);
        openAIOptions.StreamingEnabled = true;

        // Provided continuation token signals that an existing background response should be fetched.
        if (GetContinuationToken(messages, options) is { } token)
        {
            GetResponseOptions getOptions = new(token.ResponseId) { StartingAfter = token.SequenceNumber, StreamingEnabled = true };

            Debug.Assert(_getResponseStreamingAsync is not null, $"Unable to find {nameof(_getResponseStreamingAsync)} method");
            IAsyncEnumerable<StreamingResponseUpdate> getUpdates = _getResponseStreamingAsync is not null ?
                _getResponseStreamingAsync(_responseClient, getOptions, cancellationToken.ToRequestOptions(streaming: true)) :
                _responseClient.GetResponseStreamingAsync(getOptions, cancellationToken);

            return FromOpenAIStreamingResponseUpdatesAsync(getUpdates, openAIOptions, openAIConversationId, token.ResponseId, cancellationToken);
        }

        foreach (var responseItem in ToOpenAIResponseItems(messages, options))
        {
            openAIOptions.InputItems.Add(responseItem);
        }

        Debug.Assert(_createResponseStreamingAsync is not null, $"Unable to find {nameof(_createResponseStreamingAsync)} method");
        AsyncCollectionResult<StreamingResponseUpdate> createUpdates = _createResponseStreamingAsync is not null ?
            _createResponseStreamingAsync(_responseClient, openAIOptions, cancellationToken.ToRequestOptions(streaming: true)) :
            _responseClient.CreateResponseStreamingAsync(openAIOptions, cancellationToken);

        return FromOpenAIStreamingResponseUpdatesAsync(createUpdates, openAIOptions, openAIConversationId, cancellationToken: cancellationToken);
    }

    internal static async IAsyncEnumerable<ChatResponseUpdate> FromOpenAIStreamingResponseUpdatesAsync(
        IAsyncEnumerable<StreamingResponseUpdate> streamingResponseUpdates,
        CreateResponseOptions? options,
        string? conversationId,
        string? resumeResponseId = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        DateTimeOffset? createdAt = null;
        string? responseId = resumeResponseId;
        string? modelId = null;
        string? lastMessageId = null;
        ChatRole? lastRole = null;
        bool anyFunctions = false;
        ResponseStatus? latestResponseStatus = null;

        UpdateConversationId(resumeResponseId);

        await foreach (var streamingUpdate in streamingResponseUpdates.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            // Create an update populated with the current state of the response.
            ChatResponseUpdate CreateUpdate(AIContent? content = null) =>
                new(lastRole, content is not null ? [content] : null)
                {
                    ContinuationToken = CreateContinuationToken(
                        responseId!,
                        latestResponseStatus,
                        options?.BackgroundModeEnabled,
                        streamingUpdate.SequenceNumber),
                    ConversationId = conversationId,
                    CreatedAt = createdAt,
                    MessageId = lastMessageId,
                    ModelId = modelId,
                    RawRepresentation = streamingUpdate,
                    ResponseId = responseId,
                };

            switch (streamingUpdate)
            {
                case StreamingResponseCreatedUpdate createdUpdate:
                    createdAt = createdUpdate.Response.CreatedAt;
                    responseId = createdUpdate.Response.Id;
                    UpdateConversationId(responseId);
                    modelId = createdUpdate.Response.Model;
                    latestResponseStatus = createdUpdate.Response.Status;
                    goto default;

                case StreamingResponseQueuedUpdate queuedUpdate:
                    createdAt = queuedUpdate.Response.CreatedAt;
                    responseId = queuedUpdate.Response.Id;
                    UpdateConversationId(responseId);
                    modelId = queuedUpdate.Response.Model;
                    latestResponseStatus = queuedUpdate.Response.Status;
                    goto default;

                case StreamingResponseInProgressUpdate inProgressUpdate:
                    createdAt = inProgressUpdate.Response.CreatedAt;
                    responseId = inProgressUpdate.Response.Id;
                    UpdateConversationId(responseId);
                    modelId = inProgressUpdate.Response.Model;
                    latestResponseStatus = inProgressUpdate.Response.Status;
                    goto default;

                case StreamingResponseIncompleteUpdate incompleteUpdate:
                    createdAt = incompleteUpdate.Response.CreatedAt;
                    responseId = incompleteUpdate.Response.Id;
                    UpdateConversationId(responseId);
                    modelId = incompleteUpdate.Response.Model;
                    latestResponseStatus = incompleteUpdate.Response.Status;
                    goto default;

                case StreamingResponseFailedUpdate failedUpdate:
                    createdAt = failedUpdate.Response.CreatedAt;
                    responseId = failedUpdate.Response.Id;
                    UpdateConversationId(responseId);
                    modelId = failedUpdate.Response.Model;
                    latestResponseStatus = failedUpdate.Response.Status;
                    goto default;

                case StreamingResponseCompletedUpdate completedUpdate:
                {
                    createdAt = completedUpdate.Response.CreatedAt;
                    responseId = completedUpdate.Response.Id;
                    UpdateConversationId(responseId);
                    modelId = completedUpdate.Response.Model;
                    latestResponseStatus = completedUpdate.Response?.Status;
                    var update = CreateUpdate(ToUsageDetails(completedUpdate.Response) is { } usage ? new UsageContent(usage) : null);
                    update.FinishReason =
                        AsFinishReason(completedUpdate.Response?.IncompleteStatusDetails?.Reason) ??
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
                            lastRole = AsChatRole(mri.Role);
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

                case StreamingResponseReasoningSummaryTextDeltaUpdate reasoningSummaryTextDeltaUpdate:
                    yield return CreateUpdate(new TextReasoningContent(reasoningSummaryTextDeltaUpdate.Delta));
                    break;

                case StreamingResponseReasoningTextDeltaUpdate reasoningTextDeltaUpdate:
                    yield return CreateUpdate(new TextReasoningContent(reasoningTextDeltaUpdate.Delta));
                    break;

                case StreamingResponseImageGenerationCallInProgressUpdate imageGenInProgress:
                    yield return CreateUpdate(new ImageGenerationToolCallContent
                    {
                        ImageId = imageGenInProgress.ItemId,
                        RawRepresentation = imageGenInProgress,
                    });
                    break;

                case StreamingResponseImageGenerationCallPartialImageUpdate streamingImageGenUpdate:
                    yield return CreateUpdate(GetImageGenerationResult(streamingImageGenUpdate, options));
                    break;

                case StreamingResponseOutputItemDoneUpdate outputItemDoneUpdate:
                    switch (outputItemDoneUpdate.Item)
                    {
                        // Translate completed ResponseItems into their corresponding abstraction representations.
                        case FunctionCallResponseItem fcri:
                            yield return CreateUpdate(OpenAIClientExtensions.ParseCallContent(fcri.FunctionArguments.ToString(), fcri.CallId, fcri.FunctionName));
                            break;

                        case McpToolCallItem mtci:
                            var mcpUpdate = CreateUpdate();
                            AddMcpToolCallContent(mtci, mcpUpdate.Contents);
                            yield return mcpUpdate;
                            break;

                        case McpToolCallApprovalRequestItem mtcari:
                            yield return CreateUpdate(new McpServerToolApprovalRequestContent(mtcari.Id, new(mtcari.Id, mtcari.ToolName, mtcari.ServerLabel)
                            {
                                Arguments = JsonSerializer.Deserialize(mtcari.ToolArguments.ToMemory().Span, OpenAIJsonContext.Default.IReadOnlyDictionaryStringObject)!,
                                RawRepresentation = mtcari,
                            })
                            {
                                RawRepresentation = mtcari,
                            });
                            break;

                        case CodeInterpreterCallResponseItem cicri:
                            var codeUpdate = CreateUpdate();
                            AddCodeInterpreterContents(cicri, codeUpdate.Contents);
                            yield return codeUpdate;
                            break;

                        // MessageResponseItems will have already had their content yielded as part of delta updates.
                        // However, those deltas didn't yield annotations. If there are any annotations, yield them now.
                        case MessageResponseItem mri when mri.Content is { Count: > 0 } mriContent && mriContent.Any(c => c.OutputTextAnnotations is { Count: > 0 }):
                            AIContent annotatedContent = new(); // do not include RawRepresentation to avoid duplication with already yielded deltas
                            foreach (var c in mriContent)
                            {
                                PopulateAnnotations(c, annotatedContent);
                            }
                            yield return CreateUpdate(annotatedContent);
                            break;

                        // For ResponseItems where we've already yielded partial deltas for the whole content,
                        // we still want to yield an update, but we don't want it to include the ResponseItem
                        // as the RawRepresentation, since if it did, when roundtripping we'd end up sending
                        // the same content twice (first from the deltas, then from the raw response item).
                        // Just yield an update without AIContent for the ResponseItem.
                        case MessageResponseItem or ReasoningResponseItem or ImageGenerationCallResponseItem:
                            yield return CreateUpdate();
                            break;

                        // For everything else, yield an AIContent for the ResponseItem.
                        default:
                            yield return CreateUpdate(new AIContent { RawRepresentation = outputItemDoneUpdate.Item });
                            break;
                    }
                    break;

                case StreamingResponseErrorUpdate errorUpdate:
                    string? errorMessage = errorUpdate.Message;
                    string? errorCode = errorUpdate.Code;
                    string? errorParam = errorUpdate.Param;

                    // Workaround for https://github.com/openai/openai-dotnet/issues/849.
                    // The OpenAI service is sending down error information in a different format
                    // than is documented and thus a different format from what the OpenAI client
                    // library deserializes. Until that's addressed such that the data is correctly
                    // propagated through the OpenAI library, if it looks like the update doesn't
                    // contain the properly deserialized error information, try accessing it
                    // directly from the underlying JSON.
                    {
                        if (string.IsNullOrEmpty(errorMessage))
                        {
                            _ = errorUpdate.Patch.TryGetValue("$.error.message"u8, out errorMessage);
                        }

                        if (string.IsNullOrEmpty(errorCode))
                        {
                            _ = errorUpdate.Patch.TryGetValue("$.error.code"u8, out errorCode);
                        }

                        if (string.IsNullOrEmpty(errorParam))
                        {
                            _ = errorUpdate.Patch.TryGetValue("$.error.param"u8, out errorParam);
                        }
                    }

                    yield return CreateUpdate(new ErrorContent(errorMessage)
                    {
                        ErrorCode = errorCode,
                        Details = errorParam,
                    });
                    break;

                case StreamingResponseRefusalDoneUpdate refusalDone:
                    yield return CreateUpdate(new ErrorContent(refusalDone.Refusal)
                    {
                        ErrorCode = nameof(ResponseContentPart.Refusal),
                    });
                    break;

                default:
                    yield return CreateUpdate();
                    break;
            }
        }

        void UpdateConversationId(string? id)
        {
            if (options?.StoredOutputEnabled is false)
            {
                conversationId = null;
            }
            else
            {
                conversationId ??= id;
            }
        }
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose.
    }

    internal static ResponseTool? ToResponseTool(AITool tool, ChatOptions? options = null)
    {
        switch (tool)
        {
            case ResponseToolAITool rtat:
                return rtat.Tool;

            case AIFunctionDeclaration aiFunction:
                return ToResponseTool(aiFunction, options);

            case HostedWebSearchTool webSearchTool:
                return new WebSearchTool
                {
                    Filters = webSearchTool.GetProperty<WebSearchToolFilters?>(nameof(WebSearchTool.Filters)),
                    SearchContextSize = webSearchTool.GetProperty<WebSearchToolContextSize?>(nameof(WebSearchTool.SearchContextSize)),
                    UserLocation = webSearchTool.GetProperty<WebSearchToolLocation?>(nameof(WebSearchTool.UserLocation)),
                };

            case HostedFileSearchTool fileSearchTool:
                return new FileSearchTool(fileSearchTool.Inputs?.OfType<HostedVectorStoreContent>().Select(c => c.VectorStoreId) ?? [])
                {
                    Filters = fileSearchTool.GetProperty<BinaryData?>(nameof(FileSearchTool.Filters)),
                    MaxResultCount = fileSearchTool.MaximumResultCount,
                    RankingOptions = fileSearchTool.GetProperty<FileSearchToolRankingOptions?>(nameof(FileSearchTool.RankingOptions)),
                };

            case HostedCodeInterpreterTool codeTool:
                return new CodeInterpreterTool(
                    new(codeTool.Inputs?.OfType<HostedFileContent>().Select(f => f.FileId).ToList() is { Count: > 0 } ids ?
                        CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration(ids) :
                        new()));

            case HostedImageGenerationTool imageGenerationTool:
                ImageGenerationOptions? igo = imageGenerationTool.Options;
                return new ImageGenerationTool
                {
                    Background = imageGenerationTool.GetProperty<ImageGenerationToolBackground?>(nameof(ImageGenerationTool.Background)),
                    InputFidelity = imageGenerationTool.GetProperty<ImageGenerationToolInputFidelity?>(nameof(ImageGenerationTool.InputFidelity)),
                    InputImageMask = imageGenerationTool.GetProperty<ImageGenerationToolInputImageMask?>(nameof(ImageGenerationTool.InputImageMask)),
                    Model = igo?.ModelId,
                    ModerationLevel = imageGenerationTool.GetProperty<ImageGenerationToolModerationLevel?>(nameof(ImageGenerationTool.ModerationLevel)),
                    OutputCompressionFactor = imageGenerationTool.GetProperty<int?>(nameof(ImageGenerationTool.OutputCompressionFactor)),
                    OutputFileFormat = igo?.MediaType is { } mediaType ?
                        mediaType switch
                        {
                            "image/png" => ImageGenerationToolOutputFileFormat.Png,
                            "image/jpeg" => ImageGenerationToolOutputFileFormat.Jpeg,
                            "image/webp" => ImageGenerationToolOutputFileFormat.Webp,
                            _ => null,
                        } :
                        null,
                    PartialImageCount = igo?.StreamingCount,
                    Quality = imageGenerationTool.GetProperty<ImageGenerationToolQuality?>(nameof(ImageGenerationTool.Quality)),
                    Size = igo?.ImageSize is { } size ?
                        new ImageGenerationToolSize(size.Width, size.Height) :
                        null,
                };

            case HostedMcpServerTool mcpTool:
                McpTool responsesMcpTool = Uri.TryCreate(mcpTool.ServerAddress, UriKind.Absolute, out Uri? serverAddressUrl) ?
                    new McpTool(mcpTool.ServerName, serverAddressUrl) :
                    new McpTool(mcpTool.ServerName, new McpToolConnectorId(mcpTool.ServerAddress));

                responsesMcpTool.ServerDescription = mcpTool.ServerDescription;
                responsesMcpTool.AuthorizationToken = mcpTool.AuthorizationToken;

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

                return responsesMcpTool;

            default:
                return null;
        }
    }

    internal static FunctionTool ToResponseTool(AIFunctionDeclaration aiFunction, ChatOptions? options = null)
    {
        bool? strictModeEnabled =
            OpenAIClientExtensions.HasStrict(aiFunction.AdditionalProperties) ??
            OpenAIClientExtensions.HasStrict(options?.AdditionalProperties);

        return new FunctionTool(
            aiFunction.Name,
            OpenAIClientExtensions.ToOpenAIFunctionParameters(aiFunction, strictModeEnabled),
            strictModeEnabled)
        {
            FunctionDescription = aiFunction.Description,
        };
    }

    /// <summary>Creates a <see cref="ChatRole"/> from a <see cref="MessageRole"/>.</summary>
    private static ChatRole AsChatRole(MessageRole? role) =>
        role switch
        {
            MessageRole.System => ChatRole.System,
            MessageRole.Developer => OpenAIClientExtensions.ChatRoleDeveloper,
            MessageRole.User => ChatRole.User,
            _ => ChatRole.Assistant,
        };

    /// <summary>Creates a <see cref="ChatFinishReason"/> from a <see cref="ResponseIncompleteStatusReason"/>.</summary>
    private static ChatFinishReason? AsFinishReason(ResponseIncompleteStatusReason? statusReason) =>
        statusReason == ResponseIncompleteStatusReason.ContentFilter ? ChatFinishReason.ContentFilter :
        statusReason == ResponseIncompleteStatusReason.MaxOutputTokens ? ChatFinishReason.Length :
        null;

    /// <summary>Converts a <see cref="ChatOptions"/> to a <see cref="CreateResponseOptions"/>.</summary>
    private CreateResponseOptions AsCreateResponseOptions(ChatOptions? options, out string? openAIConversationId)
    {
        openAIConversationId = null;

        if (options is null)
        {
            return new()
            {
                Model = _responseClient.Model,
            };
        }

        bool hasRawRco = false;
        if (options.RawRepresentationFactory?.Invoke(this) is CreateResponseOptions result)
        {
            hasRawRco = true;
        }
        else
        {
            result = new();
        }

        result.BackgroundModeEnabled ??= options.AllowBackgroundResponses;
        result.MaxOutputTokenCount ??= options.MaxOutputTokens;
        result.Model ??= options.ModelId ?? _responseClient.Model;
        result.Temperature ??= options.Temperature;
        result.TopP ??= options.TopP;

        // If the CreateResponseOptions.PreviousResponseId is already set (likely rare), then we don't need to do
        // anything with regards to Conversation, because they're mutually exclusive and we would want to ignore
        // ChatOptions.ConversationId regardless of its value. If it's null, we want to examine the CreateResponseOptions
        // instance to see if a conversation ID has already been set on it and use that conversation ID subsequently if
        // it has. If one hasn't been set, but ChatOptions.ConversationId has been set, we'll either set
        // CreateResponseOptions.Conversation if the string represents a conversation ID or else PreviousResponseId.
        if (result.PreviousResponseId is null)
        {
            bool chatOptionsHasOpenAIConversationId = OpenAIClientExtensions.IsConversationId(options.ConversationId);

            if (hasRawRco || chatOptionsHasOpenAIConversationId)
            {
                openAIConversationId = result.ConversationOptions?.ConversationId;
                if (openAIConversationId is null && chatOptionsHasOpenAIConversationId)
                {
                    result.ConversationOptions = new(options.ConversationId);
                    openAIConversationId = options.ConversationId;
                }
            }

            // If we still don't have a conversation ID, and ChatOptions.ConversationId is set, treat it as a response ID.
            if (openAIConversationId is null && options.ConversationId is { } previousResponseId)
            {
                result.PreviousResponseId = previousResponseId;
            }
        }

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
                if (ToResponseTool(tool, options) is { } responseTool)
                {
                    result.Tools.Add(responseTool);
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
                // Some AIContent items may map to ResponseItems directly. Others map to ResponseContentParts that need to be grouped together.
                // In order to preserve ordering, we yield ResponseItems as we find them, grouping ResponseContentParts between those yielded
                // items together into their own yielded item.

                List<ResponseContentPart>? parts = null;
                bool responseItemYielded = false;

                foreach (AIContent item in input.Contents)
                {
                    // Items that directly map to a ResponseItem.
                    ResponseItem? directItem = item switch
                    {
                        { RawRepresentation: ResponseItem rawRep } => rawRep,
                        McpServerToolApprovalResponseContent mcpResp => ResponseItem.CreateMcpApprovalResponseItem(mcpResp.Id, mcpResp.Approved),
                        _ => null
                    };

                    if (directItem is not null)
                    {
                        // Yield any parts already accumulated.
                        if (parts is not null)
                        {
                            yield return ResponseItem.CreateUserMessageItem(parts);
                            parts = null;
                        }

                        // Now yield the directly mapped item.
                        yield return directItem;

                        responseItemYielded = true;
                        continue;
                    }

                    // Items that map into ResponseContentParts and are grouped.
                    switch (item)
                    {
                        case AIContent when item.RawRepresentation is ResponseContentPart rawRep:
                            (parts ??= []).Add(rawRep);
                            break;

                        case TextContent textContent:
                            (parts ??= []).Add(ResponseContentPart.CreateInputTextPart(textContent.Text));
                            break;

                        case UriContent uriContent when uriContent.HasTopLevelMediaType("image"):
                            (parts ??= []).Add(ResponseContentPart.CreateInputImagePart(uriContent.Uri, GetImageDetail(item)));
                            break;

                        case DataContent dataContent when dataContent.HasTopLevelMediaType("image"):
                            (parts ??= []).Add(ResponseContentPart.CreateInputImagePart(BinaryData.FromBytes(dataContent.Data), dataContent.MediaType, GetImageDetail(item)));
                            break;

                        case DataContent dataContent when dataContent.MediaType.StartsWith("application/pdf", StringComparison.OrdinalIgnoreCase):
                            (parts ??= []).Add(ResponseContentPart.CreateInputFilePart(BinaryData.FromBytes(dataContent.Data), dataContent.MediaType, dataContent.Name ?? $"{Guid.NewGuid():N}.pdf"));
                            break;

                        case HostedFileContent fileContent:
                            (parts ??= []).Add(ResponseContentPart.CreateInputFilePart(fileContent.FileId));
                            break;

                        case ErrorContent errorContent when errorContent.ErrorCode == nameof(ResponseContentPartKind.Refusal):
                            (parts ??= []).Add(ResponseContentPart.CreateRefusalPart(errorContent.Message));
                            break;
                    }
                }

                // If we haven't accumulated any parts nor have we yielded any items, manufacture an empty input text part
                // to guarantee that every user message results in at least one ResponseItem.
                if (parts is null && !responseItemYielded)
                {
                    parts = [];
                    parts.Add(ResponseContentPart.CreateInputTextPart(string.Empty));
                    responseItemYielded = true;
                }

                // Final yield of any accumulated parts.
                if (parts is not null)
                {
                    yield return ResponseItem.CreateUserMessageItem(parts);
                    parts = null;
                }

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
                            static FunctionCallOutputResponseItem SerializeAIContent(string callId, IEnumerable<AIContent> contents)
                            {
                                List<FunctionToolCallOutputElement> elements = [];

                                foreach (var content in contents)
                                {
                                    switch (content)
                                    {
                                        case TextContent tc:
                                            elements.Add(new()
                                            {
                                                Type = "input_text",
                                                Text = tc.Text
                                            });
                                            break;

                                        case DataContent dc when dc.HasTopLevelMediaType("image"):
                                            elements.Add(new()
                                            {
                                                Type = "input_image",
                                                ImageUrl = dc.Uri
                                            });
                                            break;

                                        case DataContent dc:
                                            elements.Add(new()
                                            {
                                                Type = "input_file",
                                                FileData = dc.Uri, // contrary to the docs, file_data is expected to be a data URI, not just the base64 portion
                                                FileName = dc.Name ?? $"file_{Guid.NewGuid():N}", // contrary to the docs, file_name is required
                                            });
                                            break;

                                        case UriContent uc when uc.HasTopLevelMediaType("image"):
                                            elements.Add(new()
                                            {
                                                Type = "input_image",
                                                ImageUrl = uc.Uri.AbsoluteUri,
                                            });
                                            break;

                                        case UriContent uc:
                                            elements.Add(new()
                                            {
                                                Type = "input_file",
                                                FileUrl = uc.Uri.AbsoluteUri,
                                            });
                                            break;

                                        case HostedFileContent fc:
                                            elements.Add(new()
                                            {
                                                Type = fc.HasTopLevelMediaType("image") ? "input_image" : "input_file",
                                                FileId = fc.FileId,
                                                FileName = fc.Name,
                                            });
                                            break;

                                        default:
                                            // Fallback to serializing and storing the resulting JSON as text.
                                            try
                                            {
                                                elements.Add(new()
                                                {
                                                    Type = "input_text",
                                                    Text = JsonSerializer.Serialize(content, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(object))),
                                                });
                                            }
                                            catch (NotSupportedException)
                                            {
                                                // If the type can't be serialized, skip it.
                                            }
                                            break;
                                    }
                                }

                                FunctionCallOutputResponseItem outputItem = new(callId, string.Empty);
                                if (elements.Count > 0)
                                {
                                    outputItem.Patch.Set("$.output"u8, JsonSerializer.SerializeToUtf8Bytes(elements, OpenAIJsonContext.Default.ListFunctionToolCallOutputElement).AsSpan());
                                }

                                return outputItem;
                            }

                            switch (resultContent.Result)
                            {
                                case AIContent ac:
                                    yield return SerializeAIContent(resultContent.CallId, [ac]);
                                    break;

                                case IEnumerable<AIContent> items:
                                    yield return SerializeAIContent(resultContent.CallId, items);
                                    break;

                                default:
                                    string? result = resultContent.Result as string;
                                    if (result is null && resultContent.Result is { } resultObj)
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
                            }
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
                            yield return new ReasoningResponseItem(reasoningContent.Text)
                            {
                                EncryptedContent = reasoningContent.ProtectedData,
                            };
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

    /// <summary>Extract usage details from a <see cref="ResponseResult"/> into a <see cref="UsageDetails"/>.</summary>
    private static UsageDetails? ToUsageDetails(ResponseResult? responseResult)
    {
        UsageDetails? ud = null;
        if (responseResult?.Usage is { } usage)
        {
            ud = new()
            {
                InputTokenCount = usage.InputTokenCount,
                OutputTokenCount = usage.OutputTokenCount,
                TotalTokenCount = usage.TotalTokenCount,
                CachedInputTokenCount = usage.InputTokenDetails?.CachedTokenCount,
                ReasoningTokenCount = usage.OutputTokenDetails?.ReasoningTokenCount,
            };
        }

        return ud;
    }

    /// <summary>Converts a <see cref="UsageDetails"/> to a <see cref="ResponseTokenUsage"/>.</summary>
    internal static ResponseTokenUsage? ToResponseTokenUsage(UsageDetails? usageDetails)
    {
        ResponseTokenUsage? rtu = null;
        if (usageDetails is not null)
        {
            rtu = new()
            {
                InputTokenCount = (int?)usageDetails.InputTokenCount ?? 0,
                OutputTokenCount = (int?)usageDetails.OutputTokenCount ?? 0,
                TotalTokenCount = (int?)usageDetails.TotalTokenCount ?? 0,
                InputTokenDetails = new(),
                OutputTokenDetails = new(),
            };

            if (usageDetails.AdditionalCounts is { } additionalCounts)
            {
                if (additionalCounts.TryGetValue($"{nameof(ResponseTokenUsage.InputTokenDetails)}.{nameof(ResponseInputTokenUsageDetails.CachedTokenCount)}", out int? cachedTokenCount))
                {
                    rtu.InputTokenDetails.CachedTokenCount = cachedTokenCount.GetValueOrDefault();
                }

                if (additionalCounts.TryGetValue($"{nameof(ResponseTokenUsage.OutputTokenDetails)}.{nameof(ResponseOutputTokenUsageDetails.ReasoningTokenCount)}", out int? reasoningTokenCount))
                {
                    rtu.OutputTokenDetails.ReasoningTokenCount = reasoningTokenCount.GetValueOrDefault();
                }
            }
        }

        return rtu;
    }

    /// <summary>Convert a sequence of <see cref="ResponseContentPart"/>s to a list of <see cref="AIContent"/>.</summary>
    private static List<AIContent> ToAIContents(IEnumerable<ResponseContentPart> contents)
    {
        List<AIContent> results = [];

        foreach (ResponseContentPart part in contents)
        {
            AIContent? content;
            switch (part.Kind)
            {
                case ResponseContentPartKind.InputText or ResponseContentPartKind.OutputText:
                    TextContent text = new(part.Text);
                    PopulateAnnotations(part, text);
                    content = text;
                    break;

                case ResponseContentPartKind.InputFile or ResponseContentPartKind.InputImage:
                    content =
                        !string.IsNullOrWhiteSpace(part.InputImageFileId) ? new HostedFileContent(part.InputImageFileId) { MediaType = "image/*" } :
                        !string.IsNullOrWhiteSpace(part.InputFileId) ? new HostedFileContent(part.InputFileId) { Name = part.InputFilename } :
                        part.InputFileBytes is not null ? new DataContent(part.InputFileBytes, part.InputFileBytesMediaType ?? "application/octet-stream") { Name = part.InputFilename } :
                        null;
                    break;

                case ResponseContentPartKind.Refusal:
                    content = new ErrorContent(part.Refusal)
                    {
                        ErrorCode = nameof(ResponseContentPartKind.Refusal),
                    };
                    break;

                default:
                    content = new();
                    break;
            }

            if (content is not null)
            {
                content.RawRepresentation = part;
                results.Add(content);
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
                    case ContainerFileCitationMessageAnnotation cfcma:
                        ca.AnnotatedRegions = [new TextSpanAnnotatedRegion { StartIndex = cfcma.StartIndex, EndIndex = cfcma.EndIndex }];
                        ca.FileId = cfcma.FileId;
                        ca.Title = cfcma.Filename;
                        break;

                    case FilePathMessageAnnotation fpma:
                        ca.FileId = fpma.FileId;
                        break;

                    case FileCitationMessageAnnotation fcma:
                        ca.FileId = fcma.FileId;
                        ca.Title = fcma.Filename;
                        break;

                    case UriCitationMessageAnnotation ucma:
                        ca.AnnotatedRegions = [new TextSpanAnnotatedRegion { StartIndex = ucma.StartIndex, EndIndex = ucma.EndIndex }];
                        ca.Url = ucma.Uri;
                        ca.Title = ucma.Title;
                        break;
                }

                (destination.Annotations ??= []).Add(ca);
            }
        }
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

    /// <summary>Adds new <see cref="AIContent"/> for the specified <paramref name="cicri"/> into <paramref name="contents"/>.</summary>
    private static void AddCodeInterpreterContents(CodeInterpreterCallResponseItem cicri, IList<AIContent> contents)
    {
        contents.Add(new CodeInterpreterToolCallContent
        {
            CallId = cicri.Id,
            Inputs = !string.IsNullOrWhiteSpace(cicri.Code) ? [new DataContent(Encoding.UTF8.GetBytes(cicri.Code), "text/x-python")] : null,

            // We purposefully do not set the RawRepresentation on the HostedCodeInterpreterToolCallContent, only on the HostedCodeInterpreterToolResultContent, to avoid
            // the same CodeInterpreterCallResponseItem being included on two different AIContent instances. When these are roundtripped, we want only one
            // CodeInterpreterCallResponseItem sent back for the pair.
        });

        contents.Add(new CodeInterpreterToolResultContent
        {
            CallId = cicri.Id,
            Outputs = cicri.Outputs is { Count: > 0 } outputs ? outputs.Select<CodeInterpreterCallOutput, AIContent?>(o =>
                o switch
                {
                    CodeInterpreterCallImageOutput cicio => new UriContent(cicio.ImageUri, OpenAIClientExtensions.ImageUriToMediaType(cicio.ImageUri)) { RawRepresentation = cicio },
                    CodeInterpreterCallLogsOutput ciclo => new TextContent(ciclo.Logs) { RawRepresentation = ciclo },
                    _ => null,
                }).OfType<AIContent>().ToList() : null,
            RawRepresentation = cicri,
        });
    }

    private static void AddImageGenerationContents(ImageGenerationCallResponseItem outputItem, CreateResponseOptions? options, IList<AIContent> contents)
    {
        var imageGenTool = options?.Tools.OfType<ImageGenerationTool>().FirstOrDefault();
        string outputFormat = imageGenTool?.OutputFileFormat?.ToString() ?? "png";

        contents.Add(new ImageGenerationToolCallContent
        {
            ImageId = outputItem.Id,
        });

        contents.Add(new ImageGenerationToolResultContent
        {
            ImageId = outputItem.Id,
            RawRepresentation = outputItem,
            Outputs = [new DataContent(outputItem.ImageResultBytes, $"image/{outputFormat}")]
        });
    }

    private static ImageGenerationToolResultContent GetImageGenerationResult(StreamingResponseImageGenerationCallPartialImageUpdate update, CreateResponseOptions? options)
    {
        var imageGenTool = options?.Tools.OfType<ImageGenerationTool>().FirstOrDefault();
        var outputType = imageGenTool?.OutputFileFormat?.ToString() ?? "png";

        return new ImageGenerationToolResultContent
        {
            ImageId = update.ItemId,
            RawRepresentation = update,
            Outputs =
            [
                new DataContent(update.PartialImageBytes, $"image/{outputType}")
                {
                    AdditionalProperties = new()
                    {
                        [nameof(update.ItemId)] = update.ItemId,
                        [nameof(update.OutputIndex)] = update.OutputIndex,
                        [nameof(update.PartialImageIndex)] = update.PartialImageIndex
                    }
                }
            ]
        };
    }

    private static ResponsesClientContinuationToken? CreateContinuationToken(ResponseResult responseResult) =>
        CreateContinuationToken(
            responseId: responseResult.Id,
            responseStatus: responseResult.Status,
            isBackgroundModeEnabled: responseResult.BackgroundModeEnabled);

    private static ResponsesClientContinuationToken? CreateContinuationToken(
        string responseId,
        ResponseStatus? responseStatus,
        bool? isBackgroundModeEnabled,
        int? updateSequenceNumber = null)
    {
        if (isBackgroundModeEnabled is not true)
        {
            return null;
        }

        // Returns a continuation token for in-progress or queued responses as they are not yet complete.
        // Also returns a continuation token if there is no status but there is a sequence number,
        // which can occur for certain streaming updates related to response content part updates: response.content_part.*,
        // response.output_text.*
        if ((responseStatus is ResponseStatus.InProgress or ResponseStatus.Queued) ||
            (responseStatus is null && updateSequenceNumber is not null))
        {
            return new ResponsesClientContinuationToken(responseId)
            {
                SequenceNumber = updateSequenceNumber,
            };
        }

        // For all other statuses: completed, failed, canceled, incomplete
        // return null to indicate the operation is finished allowing the caller
        // to stop and access the final result, failure details, reason for incompletion, etc.
        return null;
    }

    private static ResponsesClientContinuationToken? GetContinuationToken(IEnumerable<ChatMessage> messages, ChatOptions? options = null)
    {
        if (options?.ContinuationToken is { } token)
        {
            if (messages.Any())
            {
                throw new InvalidOperationException("Messages are not allowed when continuing a background response using a continuation token.");
            }

            return ResponsesClientContinuationToken.FromToken(token);
        }

        return null;
    }

    private static ResponseImageDetailLevel? GetImageDetail(AIContent content)
    {
        if (content.AdditionalProperties?.TryGetValue("detail", out object? value) is true)
        {
            return value switch
            {
                string detailString => new ResponseImageDetailLevel(detailString),
                ResponseImageDetailLevel detail => detail,
                _ => null
            };
        }

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

    /// <summary>DTO for an array element in OpenAI Responses' "Function tool call output".</summary>
    internal sealed class FunctionToolCallOutputElement
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; } // input_text, input_image, or input_file

        [JsonPropertyName("text")]
        public string? Text { get; set; }

        [JsonPropertyName("image_url")]
        public string? ImageUrl { get; set; }

        [JsonPropertyName("file_id")]
        public string? FileId { get; set; }

        [JsonPropertyName("file_data")]
        public string? FileData { get; set; }

        [JsonPropertyName("file_url")]
        public string? FileUrl { get; set; }

        [JsonPropertyName("filename")]
        public string? FileName { get; set; }
    }
}

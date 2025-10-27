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

#pragma warning disable S3011 // Reflection should not be used to increase accessibility of classes, methods, or fields
#pragma warning disable S3254 // Default parameter values should not be passed as arguments
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
        _ = Throw.IfNull(messages);

        // Convert the inputs into what OpenAIResponseClient expects.
        var openAIOptions = ToOpenAIResponseCreationOptions(options);

        // Provided continuation token signals that an existing background response should be fetched.
        if (GetContinuationToken(messages, options) is { } token)
        {
            var response = await _responseClient.GetResponseAsync(token.ResponseId, cancellationToken).ConfigureAwait(false);

            return FromOpenAIResponse(response, openAIOptions);
        }

        var openAIResponseItems = ToOpenAIResponseItems(messages, options);

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
            ContinuationToken = CreateContinuationToken(openAIResponse),
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
            response.Messages = [.. ToChatMessages(openAIResponse.OutputItems, openAIOptions)];

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

    internal static IEnumerable<ChatMessage> ToChatMessages(IEnumerable<ResponseItem> items, ResponseCreationOptions? options = null)
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
                    message.Contents.Add(new McpServerToolApprovalResponseContent(mtcari.ApprovalRequestId, mtcari.Approved) { RawRepresentation = mtcari });
                    break;

                case FunctionCallOutputResponseItem functionCallOutputItem:
                    message.Contents.Add(new FunctionResultContent(functionCallOutputItem.CallId, functionCallOutputItem.FunctionOutput) { RawRepresentation = functionCallOutputItem });
                    break;

                case ImageGenerationCallResponseItem imageGenItem:
                    message.Contents.Add(GetContentFromImageGen(imageGenItem, options));
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

    private static DataContent GetContentFromImageGen(ImageGenerationCallResponseItem outputItem, ResponseCreationOptions? options)
    {
        var imageGenTool = options?.Tools.OfType<ImageGenerationTool>().FirstOrDefault();
        string outputFormat = imageGenTool?.OutputFileFormat?.ToString() ?? "png";

        return new DataContent(outputItem.GeneratedImageBytes, $"image/{outputFormat}")
        {
            RawRepresentation = outputItem
        };
    }

    private static DataContent GetContentFromImageGenPartialImageEvent(StreamingResponseImageGenerationCallPartialImageUpdate update, ResponseCreationOptions? options)
    {
        var imageGenTool = options?.Tools.OfType<ImageGenerationTool>().FirstOrDefault();
        var outputType = imageGenTool?.OutputFileFormat?.ToString() ?? "png";

        return new DataContent(update.PartialImageBytes, $"image/{outputType}")
        {
            RawRepresentation = update,
            AdditionalProperties = new()
            {
                [nameof(update.ItemId)] = update.ItemId,
                [nameof(update.OutputIndex)] = update.OutputIndex,
                [nameof(update.PartialImageIndex)] = update.PartialImageIndex
            }
        };
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        var openAIOptions = ToOpenAIResponseCreationOptions(options);

        // Provided continuation token signals that an existing background response should be fetched.
        if (GetContinuationToken(messages, options) is { } token)
        {
            IAsyncEnumerable<StreamingResponseUpdate> updates = _responseClient.GetResponseStreamingAsync(token.ResponseId, token.SequenceNumber, cancellationToken);

            return FromOpenAIStreamingResponseUpdatesAsync(updates, openAIOptions, token.ResponseId, cancellationToken);
        }

        var openAIResponseItems = ToOpenAIResponseItems(messages, options);

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
                    ContinuationToken = CreateContinuationToken(
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

                case StreamingResponseImageGenerationCallPartialImageUpdate streamingImageGenUpdate:
                    yield return CreateUpdate(GetContentFromImageGenPartialImageEvent(streamingImageGenUpdate, options));
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

    internal static ResponseTool? ToResponseTool(AITool tool, ChatOptions? options = null)
    {
        switch (tool)
        {
            case ResponseToolAITool rtat:
                return rtat.Tool;

            case AIFunctionDeclaration aiFunction:
                return ToResponseTool(aiFunction, options);

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

                return ResponseTool.CreateWebSearchTool(location, size);

            case HostedFileSearchTool fileSearchTool:
                return ResponseTool.CreateFileSearchTool(
                    fileSearchTool.Inputs?.OfType<HostedVectorStoreContent>().Select(c => c.VectorStoreId) ?? [],
                    fileSearchTool.MaximumResultCount);

            case HostedImageGenerationTool imageGenerationTool:
                return ToImageResponseTool(imageGenerationTool);

            case HostedCodeInterpreterTool codeTool:
                return ResponseTool.CreateCodeInterpreterTool(
                    new CodeInterpreterToolContainer(codeTool.Inputs?.OfType<HostedFileContent>().Select(f => f.FileId).ToList() is { Count: > 0 } ids ?
                        CodeInterpreterToolContainerConfiguration.CreateAutomaticContainerConfiguration(ids) :
                        new()));

            case HostedMcpServerTool mcpTool:
                McpTool responsesMcpTool = Uri.TryCreate(mcpTool.ServerAddress, UriKind.Absolute, out Uri? url) ?
                    ResponseTool.CreateMcpTool(
                        mcpTool.ServerName,
                        url,
                        mcpTool.AuthorizationToken,
                        mcpTool.ServerDescription) :
                    ResponseTool.CreateMcpTool(
                        mcpTool.ServerName,
                        new McpToolConnectorId(mcpTool.ServerAddress),
                        mcpTool.AuthorizationToken,
                        mcpTool.ServerDescription);

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
        bool? strict =
            OpenAIClientExtensions.HasStrict(aiFunction.AdditionalProperties) ??
            OpenAIClientExtensions.HasStrict(options?.AdditionalProperties);

        return ResponseTool.CreateFunctionTool(
            aiFunction.Name,
            OpenAIClientExtensions.ToOpenAIFunctionParameters(aiFunction, strict),
            strict,
            aiFunction.Description);
    }

    internal ResponseTool ToImageResponseTool(HostedImageGenerationTool imageGenerationTool)
    {
        ImageGenerationOptions? imageGenerationOptions = imageGenerationTool.Options;

        // A bit unusual to get an ImageGenerationTool from the ImageGenerationOptions factory, we could
        var result = imageGenerationTool.RawRepresentationFactory?.Invoke(this) as ImageGenerationTool ?? new();

        // Model: Image generation model
        if (imageGenerationOptions?.ModelId is not null && result.Model is null)
        {
            result.Model = imageGenerationOptions.ModelId;
        }

        // Size: Image dimensions (e.g., 1024x1024, 1024x1536)
        if (imageGenerationOptions?.ImageSize is not null && result.Size is null)
        {
            // Use a custom type to ensure the size is formatted correctly.
            // This is a workaround for OpenAI's specific size format requirements.
            result.Size = new ImageGenerationToolSize(
                imageGenerationOptions.ImageSize.Value.Width,
                imageGenerationOptions.ImageSize.Value.Height);
        }

        // Format: File output format
        if (imageGenerationOptions?.MediaType is not null && result.OutputFileFormat is null)
        {
            result.OutputFileFormat = imageGenerationOptions.MediaType switch
            {
                "image/png" => ImageGenerationToolOutputFileFormat.Png,
                "image/jpeg" => ImageGenerationToolOutputFileFormat.Jpeg,
                "image/webp" => ImageGenerationToolOutputFileFormat.Webp,
                _ => null,
            };
        }

        return result;
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
        result.BackgroundModeEnabled ??= options.AllowBackgroundResponses;

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
                            (parts ??= []).Add(ResponseContentPart.CreateInputImagePart(uriContent.Uri));
                            break;

                        case DataContent dataContent when dataContent.HasTopLevelMediaType("image"):
                            (parts ??= []).Add(ResponseContentPart.CreateInputImagePart(BinaryData.FromBytes(dataContent.Data), dataContent.MediaType));
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
                            string? result = resultContent.Result as string;
                            if (result is null && resultContent.Result is { } resultObj)
                            {
                                switch (resultObj)
                                {
                                    // https://github.com/openai/openai-dotnet/issues/759
                                    // Once OpenAI supports other forms of tool call outputs, special-case various AIContent types here, e.g.
                                    // case DataContent
                                    // case HostedFileContent
                                    // case IEnumerable<AIContent>
                                    // etc.

                                    default:
                                        try
                                        {
                                            result = JsonSerializer.Serialize(resultContent.Result, AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(object)));
                                        }
                                        catch (NotSupportedException)
                                        {
                                            // If the type can't be serialized, skip it.
                                        }
                                        break;
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

    private static OpenAIResponsesContinuationToken? CreateContinuationToken(OpenAIResponse openAIResponse)
    {
        return CreateContinuationToken(
            responseId: openAIResponse.Id,
            responseStatus: openAIResponse.Status,
            isBackgroundModeEnabled: openAIResponse.BackgroundModeEnabled);
    }

    private static OpenAIResponsesContinuationToken? CreateContinuationToken(
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

    private static OpenAIResponsesContinuationToken? GetContinuationToken(IEnumerable<ChatMessage> messages, ChatOptions? options = null)
    {
        if (options?.ContinuationToken is { } token)
        {
            if (messages.Any())
            {
                throw new InvalidOperationException("Messages are not allowed when continuing a background response using a continuation token.");
            }

            return OpenAIResponsesContinuationToken.FromToken(token);
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
}

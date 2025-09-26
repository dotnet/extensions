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
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;
using OpenAI.Images;
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

        // https://github.com/openai/openai-dotnet/issues/662
        // Update to avoid reflection once OpenAIResponseClient.Model is exposed publicly.
        string? model = typeof(OpenAIResponseClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(responseClient) as string;

        _metadata = new("openai", responseClient.Endpoint, model);
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
        var openAIResponseItems = ToOpenAIResponseItems(messages, options);
        var openAIOptions = ToOpenAIResponseCreationOptions(options);

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
                    if (outputItem.GetType().Name == "InternalImageGenToolCallItemResource")
                    {
                        message.Contents.Add(GetContentFromImageGen(outputItem));
                    }
                    else
                    {
                        message.Contents.Add(new() { RawRepresentation = outputItem });
                    }

                    break;
            }
        }

        if (message is not null)
        {
            yield return message;
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ResponseItem))]
    private static DataContent GetContentFromImageGen(ResponseItem outputItem)
    {
        const BindingFlags InternalBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var imageGenResultType = Type.GetType("OpenAI.Responses.InternalImageGenToolCallItemResource, OpenAI");
        if (imageGenResultType == null)
        {
            throw new InvalidOperationException("Unable to determine the type of the image generation result.");
        }

        var imageGenStatus = imageGenResultType.GetProperty("Status", InternalBindingFlags)?.GetValue(outputItem)?.ToString();
        var imageGenResult = imageGenResultType.GetProperty("Result", InternalBindingFlags)?.GetValue(outputItem) as string;

        IDictionary<string, BinaryData>? additionalRawData = imageGenResultType
            .GetProperty("SerializedAdditionalRawData", InternalBindingFlags)
            ?.GetValue(outputItem) as IDictionary<string, BinaryData>;

        // Properties
        //   background
        //   output_format
        //   quality
        //   revised_prompt
        //   size

        string outputFormat = getStringProperty("output_format") ?? "png";

        var resultBytes = Convert.FromBase64String(imageGenResult ?? string.Empty);

        return new DataContent(resultBytes, $"image/{outputFormat}")
        {
            RawRepresentation = outputItem,
            AdditionalProperties = new()
            {
                ["background"] = getStringProperty("background"),
                ["output_format"] = outputFormat,
                ["quality"] = getStringProperty("quality"),
                ["revised_prompt"] = getStringProperty("revised_prompt"),
                ["size"] = getStringProperty("size"),
                ["status"] = imageGenStatus,
            }
        };

        string? getStringProperty(string name)
        {
            if (additionalRawData?.TryGetValue(name, out var outputFormat) == true)
            {
                var stringJsonTypeInfo = (JsonTypeInfo<string>)AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(string));
                return JsonSerializer.Deserialize(outputFormat, stringJsonTypeInfo);
            }

            return null;
        }
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ResponseItem))]
    private static DataContent GetContentFromImageGenPartialImageEvent(StreamingResponseUpdate update)
    {
        const BindingFlags InternalBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var partialImageEventType = Type.GetType("OpenAI.Responses.InternalResponseImageGenCallPartialImageEvent, OpenAI");
        if (partialImageEventType == null)
        {
            throw new InvalidOperationException("Unable to determine the type of the image generation result.");
        }

        var imageGenResult = partialImageEventType.GetProperty("PartialImageB64", InternalBindingFlags)?.GetValue(update) as string;
        var imageGenItemId = partialImageEventType.GetProperty("ItemId", InternalBindingFlags)?.GetValue(update) as string;
        var imageGenOutputIndex = partialImageEventType.GetProperty("OutputIndex", InternalBindingFlags)?.GetValue(update) as int?;
        var imageGenPartialImageIndex = partialImageEventType.GetProperty("PartialImageIndex", InternalBindingFlags)?.GetValue(update) as int?;

        IDictionary<string, BinaryData>? additionalRawData = partialImageEventType
            .GetProperty("SerializedAdditionalRawData", InternalBindingFlags)
            ?.GetValue(update) as IDictionary<string, BinaryData>;

        // Properties
        //   background
        //   output_format
        //   quality
        //   revised_prompt
        //   size

        string outputFormat = getStringProperty("output_format") ?? "png";

        var resultBytes = Convert.FromBase64String(imageGenResult ?? string.Empty);

        return new DataContent(resultBytes, $"image/{outputFormat}")
        {
            RawRepresentation = update,
            AdditionalProperties = new()
            {
                ["ItemId"] = imageGenItemId,
                ["OutputIndex"] = imageGenOutputIndex,
                ["PartialImageIndex"] = imageGenPartialImageIndex,
                ["background"] = getStringProperty("background"),
                ["output_format"] = outputFormat,
                ["quality"] = getStringProperty("quality"),
                ["revised_prompt"] = getStringProperty("revised_prompt"),
                ["size"] = getStringProperty("size"),
            }
        };

        string? getStringProperty(string name)
        {
            if (additionalRawData?.TryGetValue(name, out var outputFormat) == true)
            {
                var stringJsonTypeInfo = (JsonTypeInfo<string>)AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(string));
                return JsonSerializer.Deserialize(outputFormat, stringJsonTypeInfo);
            }

            return null;
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(messages);

        var openAIResponseItems = ToOpenAIResponseItems(messages, options);
        var openAIOptions = ToOpenAIResponseCreationOptions(options);

        var streamingUpdates = _createResponseStreamingAsync is not null ?
            _createResponseStreamingAsync(_responseClient, openAIResponseItems, openAIOptions, cancellationToken.ToRequestOptions(streaming: true)) :
            _responseClient.CreateResponseStreamingAsync(openAIResponseItems, openAIOptions, cancellationToken);

        return FromOpenAIStreamingResponseUpdatesAsync(streamingUpdates, openAIOptions, cancellationToken);
    }

    internal static async IAsyncEnumerable<ChatResponseUpdate> FromOpenAIStreamingResponseUpdatesAsync(
        IAsyncEnumerable<StreamingResponseUpdate> streamingResponseUpdates, ResponseCreationOptions? options, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        DateTimeOffset? createdAt = null;
        string? responseId = null;
        string? conversationId = null;
        string? modelId = null;
        string? lastMessageId = null;
        ChatRole? lastRole = null;
        bool anyFunctions = false;

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
                };

            switch (streamingUpdate)
            {
                case StreamingResponseCreatedUpdate createdUpdate:
                    createdAt = createdUpdate.Response.CreatedAt;
                    responseId = createdUpdate.Response.Id;
                    conversationId = options?.StoredOutputEnabled is false ? null : responseId;
                    modelId = createdUpdate.Response.Model;
                    goto default;

                case StreamingResponseCompletedUpdate completedUpdate:
                {
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

                    if (streamingUpdate.GetType().Name == "InternalResponseImageGenCallPartialImageEvent")
                    {
                        yield return CreateUpdate(GetContentFromImageGenPartialImageEvent(streamingUpdate));
                    }
                    else
                    {
                        yield return CreateUpdate();
                    }

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

    internal static ResponseTool ToImageResponseTool(HostedImageGenerationTool imageGenerationTool, ChatOptions? options = null)
    {
        ImageGenerationOptions? imageGenerationOptions = null;
        if (imageGenerationTool.AdditionalProperties.TryGetValue(nameof(ImageGenerationOptions), out object? optionsObj))
        {
            imageGenerationOptions = optionsObj as ImageGenerationOptions;
        }
        else if (options?.AdditionalProperties?.TryGetValue(nameof(ImageGenerationOptions), out object? optionsObj2) ?? false)
        {
            imageGenerationOptions = optionsObj2 as ImageGenerationOptions;
        }

        var toolOptions = imageGenerationOptions?.RawRepresentationFactory?.Invoke(null!) as Dictionary<string, object> ?? new();
        toolOptions["type"] = "image_generation";

        // Size: Image dimensions (e.g., 1024x1024, 1024x1536)
        if (imageGenerationOptions?.ImageSize is not null && !toolOptions.ContainsKey("size"))
        {
            // Use a custom type to ensure the size is formatted correctly.
            // This is a workaround for OpenAI's specific size format requirements.
            toolOptions["size"] = new GeneratedImageSize(
                imageGenerationOptions.ImageSize.Value.Width,
                imageGenerationOptions.ImageSize.Value.Height).ToString();
        }

        // Format: File output format
        if (imageGenerationOptions?.MediaType is not null && !toolOptions.ContainsKey("format"))
        {
            toolOptions["output_format"] = imageGenerationOptions.MediaType switch
            {
                "image/png" => GeneratedImageFileFormat.Png.ToString(),
                "image/jpeg" => GeneratedImageFileFormat.Jpeg.ToString(),
                "image/webp" => GeneratedImageFileFormat.Webp.ToString(),
                _ => string.Empty,
            };
        }

        // unexposed properties, string unless noted
        // background: transparent, opaque, auto
        // input_fidelity: effort model exerts to match input (high, low)
        // input_image_mask: optional image mask for inpainting.  Object with property file_id string or image_url data string.
        // model: Model ID to use for image generation
        // moderation: Moderation level (auto, low)
        // output_compression: (int) Compression level (0-100%) for JPEG and WebP formats
        // partial_images: (int) Number of partial images to return (0-3)
        // quality: Rendering quality (e.g. low, medium, high)

        // Can't create the tool, but we can deserialize it from Json
        BinaryData? toolOptionsData = BinaryData.FromBytes(
            JsonSerializer.SerializeToUtf8Bytes(toolOptions, OpenAIJsonContext.Default.IDictionaryStringObject));
        return ModelReaderWriter.Read<ResponseTool>(toolOptionsData, ModelReaderWriterOptions.Json)!;
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
        result.ParallelToolCallsEnabled ??= options.AllowMultipleToolCalls;
        result.PreviousResponseId ??= options.ConversationId;
        result.Temperature ??= options.Temperature;
        result.TopP ??= options.TopP;

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

                    case HostedImageGenerationTool imageGenerationTool:
                        result.Tools.Add(ToImageResponseTool(imageGenerationTool, options));
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
                        string json;
                        if (codeTool.Inputs is { Count: > 0 } inputs)
                        {
                            string jsonArray = JsonSerializer.Serialize(
                                inputs.OfType<HostedFileContent>().Select(c => c.FileId),
                                OpenAIJsonContext.Default.IEnumerableString);
                            json = $$"""{"type":"code_interpreter","container":{"type":"auto",files:{{jsonArray}}} }""";
                        }
                        else
                        {
                            json = """{"type":"code_interpreter","container":{"type":"auto"}}""";
                        }

                        result.Tools.Add(ModelReaderWriter.Read<ResponseTool>(BinaryData.FromString(json)));
                        break;

                    case HostedMcpServerTool mcpTool:
                        McpTool responsesMcpTool = ResponseTool.CreateMcpTool(
                            mcpTool.ServerName,
                            mcpTool.Url,
                            mcpTool.Headers);

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
                            // BUG https://github.com/openai/openai-dotnet/issues/664: Needs to be able to set an approvalRequestId
                            yield return ResponseItem.CreateMcpApprovalRequestItem(
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

    /// <summary>Provides an <see cref="AITool"/> wrapper for a <see cref="ResponseTool"/>.</summary>
    internal sealed class ResponseToolAITool(ResponseTool tool) : AITool
    {
        public ResponseTool Tool => tool;
        public override string Name => Tool.GetType().Name;
    }
}

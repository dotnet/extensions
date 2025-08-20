// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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
#pragma warning disable S3604 // Member initializer values should not be redundant
#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IChatClient"/> for an <see cref="OpenAIResponseClient"/>.</summary>
internal sealed class OpenAIResponsesChatClient : IChatClient
{
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

        // https://github.com/openai/openai-dotnet/issues/215
        // The endpoint and model aren't currently exposed, so use reflection to get at them, temporarily. Once packages
        // implement the abstractions directly rather than providing adapters on top of the public APIs,
        // the package can provide such implementations separate from what's exposed in the public API.
        Uri providerUrl = typeof(OpenAIResponseClient).GetField("_endpoint", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(responseClient) as Uri ?? OpenAIClientExtensions.DefaultOpenAIEndpoint;
        string? model = typeof(OpenAIResponseClient).GetField("_model", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(responseClient) as string;

        _metadata = new("openai", providerUrl, model);
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
        var openAIResponse = (await _responseClient.CreateResponseAsync(openAIResponseItems, openAIOptions, cancellationToken).ConfigureAwait(false)).Value;

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

                case ReasoningResponseItem reasoningItem when reasoningItem.GetSummaryText() is string summary:
                    message.Contents.Add(new TextReasoningContent(summary) { RawRepresentation = outputItem });
                    break;

                case FunctionCallResponseItem functionCall:
                    var fcc = OpenAIClientExtensions.ParseCallContent(functionCall.FunctionArguments, functionCall.CallId, functionCall.FunctionName);
                    fcc.RawRepresentation = outputItem;
                    message.Contents.Add(fcc);
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

        var streamingUpdates = _responseClient.CreateResponseStreamingAsync(openAIResponseItems, openAIOptions, cancellationToken);

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
        Dictionary<int, MessageResponseItem> outputIndexToMessages = [];
        Dictionary<int, FunctionCallInfo>? functionCallInfos = null;

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
                        (functionCallInfos is not null ? ChatFinishReason.ToolCalls :
                        ChatFinishReason.Stop);
                    yield return update;
                    break;
                }

                case StreamingResponseOutputItemAddedUpdate outputItemAddedUpdate:
                    switch (outputItemAddedUpdate.Item)
                    {
                        case MessageResponseItem mri:
                            outputIndexToMessages[outputItemAddedUpdate.OutputIndex] = mri;
                            break;

                        case FunctionCallResponseItem fcri:
                            (functionCallInfos ??= [])[outputItemAddedUpdate.OutputIndex] = new(fcri);
                            break;
                    }

                    goto default;

                case StreamingResponseOutputItemDoneUpdate outputItemDoneUpdate:
                    _ = outputIndexToMessages.Remove(outputItemDoneUpdate.OutputIndex);

                    if (outputItemDoneUpdate.Item is MessageResponseItem item &&
                        item.Content is { Count: > 0 } content &&
                        content.Any(c => c.OutputTextAnnotations is { Count: > 0 }))
                    {
                        AIContent annotatedContent = new();
                        foreach (var c in content)
                        {
                            PopulateAnnotations(c, annotatedContent);
                        }

                        yield return CreateUpdate(annotatedContent);
                        break;
                    }

                    goto default;

                case StreamingResponseOutputTextDeltaUpdate outputTextDeltaUpdate:
                {
                    _ = outputIndexToMessages.TryGetValue(outputTextDeltaUpdate.OutputIndex, out MessageResponseItem? messageItem);
                    lastMessageId = messageItem?.Id;
                    lastRole = ToChatRole(messageItem?.Role);

                    yield return CreateUpdate(new TextContent(outputTextDeltaUpdate.Delta));
                    break;
                }

                case StreamingResponseFunctionCallArgumentsDeltaUpdate functionCallArgumentsDeltaUpdate:
                {
                    if (functionCallInfos?.TryGetValue(functionCallArgumentsDeltaUpdate.OutputIndex, out FunctionCallInfo? callInfo) is true)
                    {
                        _ = (callInfo.Arguments ??= new()).Append(functionCallArgumentsDeltaUpdate.Delta);
                    }

                    goto default;
                }

                case StreamingResponseFunctionCallArgumentsDoneUpdate functionCallOutputDoneUpdate:
                {
                    if (functionCallInfos?.TryGetValue(functionCallOutputDoneUpdate.OutputIndex, out FunctionCallInfo? callInfo) is true)
                    {
                        _ = functionCallInfos.Remove(functionCallOutputDoneUpdate.OutputIndex);

                        var fcc = OpenAIClientExtensions.ParseCallContent(
                            callInfo.Arguments?.ToString() ?? string.Empty,
                            callInfo.ResponseItem.CallId,
                            callInfo.ResponseItem.FunctionName);

                        lastMessageId = callInfo.ResponseItem.Id;
                        lastRole = ChatRole.Assistant;

                        yield return CreateUpdate(fcc);
                        break;
                    }

                    goto default;
                }

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

    internal static ResponseTool ToResponseTool(AIFunction aiFunction, ChatOptions? options = null)
    {
        bool? strict =
            OpenAIClientExtensions.HasStrict(aiFunction.AdditionalProperties) ??
            OpenAIClientExtensions.HasStrict(options?.AdditionalProperties);

        return ResponseTool.CreateFunctionTool(
            aiFunction.Name,
            aiFunction.Description,
            OpenAIClientExtensions.ToOpenAIFunctionParameters(aiFunction, strict),
            strict ?? false);
    }

    internal static ResponseTool ToImageResponseTool(ImageGenerationTool imageGenerationTool, ChatOptions? options = null)
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
                    case AIFunction aiFunction:
                        result.Tools.Add(ToResponseTool(aiFunction, options));
                        break;

                    case ImageGenerationTool imageGenerationTool:
                        result.Tools.Add(ToImageResponseTool(imageGenerationTool, options));
                        break;

                    case HostedWebSearchTool webSearchTool:
                        WebSearchUserLocation? location = null;
                        if (webSearchTool.AdditionalProperties.TryGetValue(nameof(WebSearchUserLocation), out object? objLocation))
                        {
                            location = objLocation as WebSearchUserLocation;
                        }

                        WebSearchContextSize? size = null;
                        if (webSearchTool.AdditionalProperties.TryGetValue(nameof(WebSearchContextSize), out object? objSize) &&
                            objSize is WebSearchContextSize)
                        {
                            size = (WebSearchContextSize)objSize;
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
                            json = """{"type":"code_interpreter","container":"auto"}""";
                        }

                        result.Tools.Add(ModelReaderWriter.Read<ResponseTool>(BinaryData.FromString(json)));
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

        if (result.TextOptions is null)
        {
            if (options.ResponseFormat is ChatResponseFormatText)
            {
                result.TextOptions = new()
                {
                    TextFormat = ResponseTextFormat.CreateTextFormat()
                };
            }
            else if (options.ResponseFormat is ChatResponseFormatJson jsonFormat)
            {
                result.TextOptions = new()
                {
                    TextFormat = OpenAIClientExtensions.StrictSchemaTransformCache.GetOrCreateTransformedSchema(jsonFormat) is { } jsonSchema ?
                        ResponseTextFormat.CreateJsonSchemaFormat(
                            jsonFormat.SchemaName ?? "json_schema",
                            BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(jsonSchema, OpenAIJsonContext.Default.JsonElement)),
                            jsonFormat.SchemaDescription,
                            OpenAIClientExtensions.HasStrict(options.AdditionalProperties)) :
                        ResponseTextFormat.CreateJsonObjectFormat(),
                };
            }
        }

        return result;
    }

    /// <summary>Convert a sequence of <see cref="ChatMessage"/>s to <see cref="ResponseItem"/>s.</summary>
    internal static IEnumerable<ResponseItem> ToOpenAIResponseItems(IEnumerable<ChatMessage> inputs, ChatOptions? options)
    {
        _ = options; // currently unused

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
                            yield return ResponseItem.CreateReasoningItem(reasoningContent.Text);
                            break;

                        case FunctionCallContent callContent:
                            yield return ResponseItem.CreateFunctionCallItem(
                                callContent.CallId,
                                callContent.Name,
                                BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(
                                    callContent.Arguments,
                                    AIJsonUtilities.DefaultOptions.GetTypeInfo(typeof(IDictionary<string, object?>)))));
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
                (destination.Annotations ??= []).Add(new CitationAnnotation
                {
                    RawRepresentation = ota,
                    AnnotatedRegions = [new TextSpanAnnotatedRegion { StartIndex = ota.UriCitationStartIndex, EndIndex = ota.UriCitationEndIndex }],
                    Title = ota.UriCitationTitle,
                    Url = ota.UriCitationUri,
                    FileId = ota.FileCitationFileId ?? ota.FilePathFileId,
                });
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

    /// <summary>POCO representing function calling info.</summary>
    /// <remarks>Used to concatenation information for a single function call from across multiple streaming updates.</remarks>
    private sealed class FunctionCallInfo(FunctionCallResponseItem item)
    {
        public readonly FunctionCallResponseItem ResponseItem = item;
        public StringBuilder? Arguments;
    }
}

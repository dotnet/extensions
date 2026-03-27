// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using OpenAI.Conversations;

#pragma warning disable S1006 // Add the default parameter value defined in the overridden method
#pragma warning disable S3254 // Remove this default value assigned to parameter

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IHostedConversationClient"/> for an OpenAI <see cref="ConversationClient"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIHostedConversation, UrlFormat = DiagnosticIds.UrlFormat)]
internal sealed class OpenAIHostedConversationClient : IHostedConversationClient
{
    /// <summary>Metadata about the client.</summary>
    private readonly HostedConversationClientMetadata _metadata;

    /// <summary>The underlying <see cref="ConversationClient"/>.</summary>
    private readonly ConversationClient _conversationClient;

    /// <summary>Initializes a new instance of the <see cref="OpenAIHostedConversationClient"/> class.</summary>
    /// <param name="conversationClient">The underlying client.</param>
    /// <exception cref="ArgumentNullException"><paramref name="conversationClient"/> is <see langword="null"/>.</exception>
    public OpenAIHostedConversationClient(ConversationClient conversationClient)
    {
        _conversationClient = Throw.IfNull(conversationClient);
        _metadata = new("openai", conversationClient.Endpoint);
    }

    /// <inheritdoc />
    public async Task<HostedConversation> CreateAsync(
        HostedConversationClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        using BinaryContent content = CreateOrGetCreatePayload(options);
        RequestOptions requestOptions = cancellationToken.ToRequestOptions(streaming: false);

        ClientResult result = await _conversationClient.CreateConversationAsync(content, requestOptions).ConfigureAwait(false);

        return ParseConversation(result);
    }

    /// <inheritdoc />
    public async Task<HostedConversation> GetAsync(
        string conversationId,
        HostedConversationClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(conversationId);

        RequestOptions requestOptions = cancellationToken.ToRequestOptions(streaming: false);

        ClientResult result = await _conversationClient.GetConversationAsync(conversationId, requestOptions).ConfigureAwait(false);

        return ParseConversation(result);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        string conversationId,
        HostedConversationClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(conversationId);

        RequestOptions requestOptions = cancellationToken.ToRequestOptions(streaming: false);
        _ = await _conversationClient.DeleteConversationAsync(conversationId, requestOptions).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddMessagesAsync(
        string conversationId,
        IEnumerable<ChatMessage> messages,
        HostedConversationClientOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(conversationId);
        _ = Throw.IfNull(messages);

        using BinaryContent content = CreateBinaryContent(static (writer, msgs) => WriteItemsPayload(writer, msgs), messages);
        RequestOptions requestOptions = cancellationToken.ToRequestOptions(streaming: false);

        _ = await _conversationClient.CreateConversationItemsAsync(conversationId, content, null, requestOptions).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatMessage> GetMessagesAsync(
        string conversationId,
        HostedConversationClientOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(conversationId);

        int? limit = options?.Limit;
        RequestOptions requestOptions = cancellationToken.ToRequestOptions(streaming: false);

        // Manual pagination: the SDK's GetRawPagesAsync() only yields a single page because
        // GetContinuationToken returns null in the generated collection result class.
        // We loop using the cursor-based 'after' parameter until all items are retrieved.
        string? after = null;

        do
        {
            AsyncCollectionResult pages = _conversationClient.GetConversationItemsAsync(
                conversationId, limit: limit, order: null, after: after, include: null, options: requestOptions);

            bool hasMore = false;
            string? lastId = null;

            await foreach (ClientResult page in pages.GetRawPagesAsync().ConfigureAwait(false))
            {
                using JsonDocument doc = JsonDocument.Parse(page.GetRawResponse().Content);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("has_more", out JsonElement hasMoreElement) &&
                    hasMoreElement.ValueKind == JsonValueKind.True)
                {
                    hasMore = true;
                }

                if (root.TryGetProperty("last_id", out JsonElement lastIdElement))
                {
                    lastId = lastIdElement.GetString();
                }

                if (!root.TryGetProperty("data", out JsonElement dataElement))
                {
                    continue;
                }

                foreach (JsonElement item in dataElement.EnumerateArray())
                {
                    if (TryConvertItemToChatMessage(item) is { } message)
                    {
                        yield return message;
                    }
                }
            }

            after = hasMore ? lastId : null;
        }
        while (after is not null);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        _ = Throw.IfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(HostedConversationClientMetadata) ? _metadata :
            serviceType == typeof(ConversationClient) ? _conversationClient :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <summary>Creates a <see cref="BinaryContent"/> for the create conversation request, using the raw representation factory if available.</summary>
    private BinaryContent CreateOrGetCreatePayload(HostedConversationClientOptions? options)
    {
        if (options?.RawRepresentationFactory?.Invoke(this) is BinaryContent rawContent)
        {
            return rawContent;
        }

        return CreateBinaryContent(static (writer, opts) => WriteCreatePayload(writer, opts), options);
    }

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IHostedConversationClient interface.
    }

    /// <summary>Creates a <see cref="BinaryContent"/> from a JSON writing action.</summary>
    private static BinaryContent CreateBinaryContent<TState>(Action<Utf8JsonWriter, TState> writeAction, TState state)
    {
        using MemoryStream stream = new();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writeAction(writer, state);
        }

        return BinaryContent.Create(new BinaryData(stream.ToArray()));
    }

    /// <summary>Writes the JSON payload for creating a conversation.</summary>
    private static void WriteCreatePayload(Utf8JsonWriter writer, HostedConversationClientOptions? options)
    {
        writer.WriteStartObject();

        if (options?.AdditionalProperties is { Count: > 0 } additionalProperties)
        {
            // Map "metadata" from AdditionalProperties if present as a dictionary of string values.
            if (additionalProperties.TryGetValue("metadata", out object? metadataObj) &&
                metadataObj is AdditionalPropertiesDictionary<string> metadata &&
                metadata.Count > 0)
            {
                writer.WritePropertyName("metadata");
                writer.WriteStartObject();
                foreach (var kvp in metadata)
                {
                    writer.WriteString(kvp.Key, kvp.Value);
                }

                writer.WriteEndObject();
            }
        }

        writer.WriteEndObject();
    }

    /// <summary>Writes the JSON payload for adding items to a conversation.</summary>
    private static void WriteItemsPayload(Utf8JsonWriter writer, IEnumerable<ChatMessage> messages)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("items");
        WriteMessagesArray(writer, messages);
        writer.WriteEndObject();
    }

    /// <summary>Writes a JSON array of conversation items from a collection of <see cref="ChatMessage"/> instances.</summary>
    private static void WriteMessagesArray(Utf8JsonWriter writer, IEnumerable<ChatMessage> messages)
    {
        writer.WriteStartArray();

        foreach (ChatMessage message in messages)
        {
            string role = message.Role == ChatRole.Assistant ? "assistant" :
                          message.Role == ChatRole.System ? "system" :
                          message.Role == OpenAIClientExtensions.ChatRoleDeveloper ? "developer" :
                          "user";

            writer.WriteStartObject();
            writer.WriteString("type", "message");
            writer.WriteString("role", role);
            writer.WritePropertyName("content");
            writer.WriteStartArray();

            bool hasContent = false;
            foreach (AIContent aiContent in message.Contents)
            {
                switch (aiContent)
                {
                    case TextContent textContent:
                        writer.WriteStartObject();
                        writer.WriteString("type", message.Role == ChatRole.Assistant ? "output_text" : "input_text");
                        writer.WriteString("text", textContent.Text);
                        writer.WriteEndObject();
                        hasContent = true;
                        break;

                    case UriContent uriContent when uriContent.HasTopLevelMediaType("image"):
                        writer.WriteStartObject();
                        writer.WriteString("type", "input_image");
                        writer.WriteString("image_url", uriContent.Uri.ToString());
                        writer.WriteEndObject();
                        hasContent = true;
                        break;

                    case DataContent dataContent when dataContent.HasTopLevelMediaType("image"):
                        writer.WriteStartObject();
                        writer.WriteString("type", "input_image");
                        writer.WriteString("image_url", dataContent.Uri);
                        writer.WriteEndObject();
                        hasContent = true;
                        break;

                    case HostedFileContent fileContent:
                        writer.WriteStartObject();
                        if (fileContent.HasTopLevelMediaType("image"))
                        {
                            writer.WriteString("type", "input_image");
                            writer.WritePropertyName("image_file");
                            writer.WriteStartObject();
                            writer.WriteString("file_id", fileContent.FileId);
                            writer.WriteEndObject();
                        }
                        else
                        {
                            writer.WriteString("type", "input_file");
                            writer.WriteString("file_id", fileContent.FileId);
                        }

                        writer.WriteEndObject();
                        hasContent = true;
                        break;
                }
            }

            // If no structured content parts were produced, fall back to the text property.
            if (!hasContent)
            {
                string? text = message.Text;
                if (text is not null)
                {
                    writer.WriteStartObject();
                    writer.WriteString("type", message.Role == ChatRole.Assistant ? "output_text" : "input_text");
                    writer.WriteString("text", text);
                    writer.WriteEndObject();
                }
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    /// <summary>Parses a <see cref="HostedConversation"/> from a <see cref="ClientResult"/>.</summary>
    private static HostedConversation ParseConversation(ClientResult result)
    {
        using JsonDocument doc = JsonDocument.Parse(result.GetRawResponse().Content);
        JsonElement root = doc.RootElement;

        var conversation = new HostedConversation
        {
            ConversationId = root.TryGetProperty("id", out JsonElement idElement) ? idElement.GetString() : null,
            CreatedAt = root.TryGetProperty("created_at", out JsonElement createdAtElement) && createdAtElement.ValueKind == JsonValueKind.Number ? DateTimeOffset.FromUnixTimeSeconds(createdAtElement.GetInt64()) : null,
            RawRepresentation = result,
        };

        if (ParseMetadata(root) is { } metadata)
        {
            conversation.AdditionalProperties ??= new();
            conversation.AdditionalProperties["metadata"] = metadata;
        }

        return conversation;
    }

    /// <summary>Attempts to convert a JSON element representing a conversation item to a <see cref="ChatMessage"/>.</summary>
    private static ChatMessage? TryConvertItemToChatMessage(JsonElement item)
    {
        if (!item.TryGetProperty("type", out JsonElement typeElement))
        {
            return null;
        }

        string? type = typeElement.GetString();
        if (type != "message")
        {
            return null;
        }

        ChatRole role = ChatRole.User;
        if (item.TryGetProperty("role", out JsonElement roleElement))
        {
            string? roleStr = roleElement.GetString();
            role = roleStr switch
            {
                "assistant" => ChatRole.Assistant,
                "system" => ChatRole.System,
                "developer" => OpenAIClientExtensions.ChatRoleDeveloper,
                "tool" => ChatRole.Tool,
                _ => ChatRole.User,
            };
        }

        var message = new ChatMessage(role, (string?)null);

        if (item.TryGetProperty("id", out JsonElement idElement))
        {
            message.MessageId = idElement.GetString();
        }

        if (item.TryGetProperty("content", out JsonElement contentElement) && contentElement.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement part in contentElement.EnumerateArray())
            {
                if (!part.TryGetProperty("type", out JsonElement partType))
                {
                    continue;
                }

                string? partTypeStr = partType.GetString();
                switch (partTypeStr)
                {
                    case "input_text" or "output_text" or "text":
                        if (part.TryGetProperty("text", out JsonElement textElement))
                        {
                            message.Contents.Add(new TextContent(textElement.GetString()));
                        }

                        break;

                    case "refusal":
                        if (part.TryGetProperty("refusal", out JsonElement refusalElement))
                        {
                            message.Contents.Add(new ErrorContent(refusalElement.GetString()) { ErrorCode = "Refusal" });
                        }

                        break;

                    case "input_image":
                        if (part.TryGetProperty("image_url", out JsonElement imageUrlElement) &&
                            imageUrlElement.GetString() is { } imageUrl &&
                            Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? imageUri))
                        {
                            message.Contents.Add(new UriContent(imageUri, OpenAIClientExtensions.ImageUriToMediaType(imageUri)));
                        }
                        else if (part.TryGetProperty("file_id", out JsonElement fileIdElement) &&
                                 fileIdElement.GetString() is { } fileId)
                        {
                            message.Contents.Add(new HostedFileContent(fileId) { MediaType = "image/*" });
                        }

                        break;

                    case "input_file":
                        if (part.TryGetProperty("file_id", out JsonElement inputFileIdElement) &&
                            inputFileIdElement.GetString() is { } inputFileId)
                        {
                            string? filename = part.TryGetProperty("filename", out JsonElement filenameElement) ? filenameElement.GetString() : null;
                            message.Contents.Add(new HostedFileContent(inputFileId) { Name = filename });
                        }

                        break;
                }
            }
        }

        return message;
    }

    /// <summary>Parses metadata from a JSON element.</summary>
    private static AdditionalPropertiesDictionary<string>? ParseMetadata(JsonElement root)
    {
        if (!root.TryGetProperty("metadata", out JsonElement metadataElement) ||
            metadataElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var metadata = new AdditionalPropertiesDictionary<string>();
        foreach (JsonProperty property in metadataElement.EnumerateObject())
        {
            metadata[property.Name] = property.Value.GetString() ?? string.Empty;
        }

        return metadata.Count > 0 ? metadata : null;
    }
}

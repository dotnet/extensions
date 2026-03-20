// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;
using OpenAI.Videos;

namespace Microsoft.Extensions.AI;

/// <summary>Represents an <see cref="IVideoGenerator"/> for an OpenAI <see cref="VideoClient"/>.</summary>
/// <remarks>
/// <para>
/// This implementation uses the OpenAI video generation API. Video generation is asynchronous:
/// a generation job is created, polled for completion, and then the video content is downloaded.
/// </para>
/// <para>The operation chosen depends on the request contents and options:</para>
/// <list type="bullet">
/// <item><description>
/// <b>Text-to-video</b>: When <see cref="VideoGenerationRequest.OriginalMedia"/> is
/// <see langword="null"/> and no routing keys are set, creates a new video from the
/// text prompt via <c>POST /videos</c>.
/// </description></item>
/// <item><description>
/// <b>Image-to-video</b>: When <see cref="VideoGenerationRequest.OriginalMedia"/>
/// contains image content (e.g., <c>image/png</c>), uses the image as an
/// <c>input_reference</c> to guide new video creation via <c>POST /videos</c>.
/// A <see cref="UriContent"/> sends the image URL in JSON; a <see cref="DataContent"/>
/// uploads the image bytes via multipart/form-data.
/// </description></item>
/// <item><description>
/// <b>Edit by video ID</b>: When <c>edit_video_id</c> is set in
/// <see cref="VideoGenerationOptions.AdditionalProperties"/>, edits the specified
/// video via <c>POST /videos/edits</c>.
/// </description></item>
/// <item><description>
/// <b>Edit by upload</b>: When <see cref="VideoGenerationRequest.OriginalMedia"/>
/// contains video content (e.g., <c>video/mp4</c>), uploads the video for editing
/// via <c>POST /videos/edits</c> with multipart/form-data.
/// </description></item>
/// <item><description>
/// <b>Extend</b>: When <c>extend_video_id</c> is set in
/// <see cref="VideoGenerationOptions.AdditionalProperties"/>, extends the completed
/// video via <c>POST /videos/extensions</c>.
/// </description></item>
/// </list>
/// <para>
/// Character IDs can be included in the create request by passing a <c>characters</c>
/// key in <see cref="VideoGenerationOptions.AdditionalProperties"/> as a JSON array.
/// Characters are reusable visual assets created separately via
/// <c>POST /videos/characters</c>.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOpenAIVideoClient)]
internal sealed class OpenAIVideoGenerator : IVideoGenerator
{
    /// <summary>Default polling interval for checking video generation status.</summary>
    private static readonly TimeSpan _defaultPollingInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Well-known <see cref="VideoGenerationOptions.AdditionalProperties"/> key that routes the
    /// request to <c>POST /videos/edits</c>. The value should be the video ID to edit.
    /// </summary>
    internal const string EditVideoIdKey = "edit_video_id";

    /// <summary>
    /// Well-known <see cref="VideoGenerationOptions.AdditionalProperties"/> key that routes the
    /// request to <c>POST /videos/extensions</c>. The value should be the completed video ID to extend.
    /// </summary>
    internal const string ExtendVideoIdKey = "extend_video_id";

    /// <summary>Metadata about the client.</summary>
    private readonly VideoGeneratorMetadata _metadata;

    /// <summary>The underlying <see cref="VideoClient" />.</summary>
    private readonly VideoClient _videoClient;

    /// <summary>The model to use for video generation.</summary>
    private readonly string? _defaultModelId;

    /// <summary>Initializes a new instance of the <see cref="OpenAIVideoGenerator"/> class for the specified <see cref="VideoClient"/>.</summary>
    /// <param name="videoClient">The underlying client.</param>
    /// <param name="modelId">The model ID to use for video generation (e.g. "sora-2").</param>
    /// <exception cref="ArgumentNullException"><paramref name="videoClient"/> is <see langword="null"/>.</exception>
    public OpenAIVideoGenerator(VideoClient videoClient, string? modelId = null)
    {
        _videoClient = Throw.IfNull(videoClient);
        _defaultModelId = modelId;

        _metadata = new("openai", videoClient.Endpoint, modelId);
    }

    /// <inheritdoc />
    public async Task<VideoGenerationResponse> GenerateAsync(
        VideoGenerationRequest request,
        VideoGenerationOptions? options = null,
        IProgress<VideoGenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _ = Throw.IfNull(request);

        string? prompt = request.Prompt;
        _ = Throw.IfNull(prompt);

        string modelId = options?.ModelId ?? _defaultModelId ?? "sora-2";

        // Check for routing keys in AdditionalProperties
        string? editVideoId = GetStringAdditionalProperty(options, EditVideoIdKey);
        string? extendVideoId = GetStringAdditionalProperty(options, ExtendVideoIdKey);

        // Determine OriginalMedia type (only when no routing keys override the operation)
        DataContent? videoEditContent = null;
        DataContent? imageReferenceData = null;
        UriContent? imageReferenceUri = null;

        if (editVideoId is null && extendVideoId is null &&
            request.OriginalMedia is { } originalMedia)
        {
            foreach (AIContent media in originalMedia)
            {
                if (media is DataContent dc && dc.Data.Length > 0)
                {
                    if (IsVideoMediaType(dc.MediaType))
                    {
                        videoEditContent = dc;
                    }
                    else if (IsImageMediaType(dc.MediaType))
                    {
                        imageReferenceData = dc;
                    }

                    break;
                }

                if (media is UriContent uc && IsImageMediaType(uc.MediaType))
                {
                    imageReferenceUri = uc;
                    break;
                }
            }
        }

        // Route to the appropriate endpoint and create the video generation job
        RequestOptions reqOpts = new() { CancellationToken = cancellationToken };
        ClientResult createResult;

        if (extendVideoId is not null)
        {
            // POST /videos/extensions — extend a completed video
            JsonObject body = new()
            {
                ["prompt"] = prompt,
                ["video"] = new JsonObject { ["id"] = extendVideoId },
            };

            if (options?.Duration is TimeSpan extDuration)
            {
                body["seconds"] = (int)extDuration.TotalSeconds;
            }

            ForwardAdditionalProperties(body, options);
            using BinaryContent extendContent = BinaryContent.Create(
                SerializeJsonToUtf8(body));
            using PipelineMessage extendMsg = CreatePipelineRequest(
                _videoClient, "/videos/extensions", extendContent,
                "application/json", reqOpts);
            await _videoClient.Pipeline.SendAsync(extendMsg).ConfigureAwait(false);
            createResult = ClientResult.FromResponse(extendMsg.Response!);
        }
        else if (editVideoId is not null)
        {
            // POST /videos/edits — edit an existing video by ID
            JsonObject body = new()
            {
                ["prompt"] = prompt,
                ["video"] = new JsonObject { ["id"] = editVideoId },
            };

            ForwardAdditionalProperties(body, options);
            using BinaryContent editContent = BinaryContent.Create(
                SerializeJsonToUtf8(body));
            using PipelineMessage editMsg = CreatePipelineRequest(
                _videoClient, "/videos/edits", editContent,
                "application/json", reqOpts);
            await _videoClient.Pipeline.SendAsync(editMsg).ConfigureAwait(false);
            createResult = ClientResult.FromResponse(editMsg.Response!);
        }
        else if (videoEditContent is not null)
        {
            // POST /videos/edits — edit an uploaded video via multipart
            JsonObject formFields = new()
            {
                ["prompt"] = prompt,
                ["model"] = modelId,
            };

            ForwardAdditionalProperties(formFields, options);
            using BinaryContent editMultipart = BuildMultipartContent(
                formFields, videoEditContent, "video",
                out string editContentType);
            using PipelineMessage editUploadMsg = CreatePipelineRequest(
                _videoClient, "/videos/edits", editMultipart,
                editContentType, reqOpts);
            await _videoClient.Pipeline.SendAsync(editUploadMsg).ConfigureAwait(false);
            createResult = ClientResult.FromResponse(editUploadMsg.Response!);
        }
        else
        {
            // POST /videos — text-to-video or image-to-video
            JsonObject requestBody = new()
            {
                ["model"] = modelId,
                ["prompt"] = prompt,
            };

            if (options?.VideoSize is Size size)
            {
                requestBody["size"] = $"{size.Width}x{size.Height}";
            }

            if (options?.Duration is TimeSpan duration)
            {
                requestBody["seconds"] = (int)duration.TotalSeconds;
            }

            if (options?.Count is int count && count > 1)
            {
                requestBody["n"] = count;
            }

            ForwardAdditionalProperties(requestBody, options);

            if (imageReferenceUri is not null)
            {
                requestBody["input_reference"] = new JsonObject
                {
                    ["image_url"] = imageReferenceUri.Uri.ToString(),
                };
            }

            if (imageReferenceData is not null)
            {
                using BinaryContent multipartContent = BuildMultipartContent(
                    requestBody, imageReferenceData, "input_reference",
                    out string multipartContentType);
                createResult = await _videoClient.CreateVideoAsync(
                    multipartContent, multipartContentType,
                    reqOpts).ConfigureAwait(false);
            }
            else
            {
                using BinaryContent content = BinaryContent.Create(
                    SerializeJsonToUtf8(requestBody));
                createResult = await _videoClient.CreateVideoAsync(
                    content, "application/json", reqOpts).ConfigureAwait(false);
            }
        }

        // Parse the creation response to get the video ID and status
        using JsonDocument createDoc = JsonDocument.Parse(
            createResult.GetRawResponse().Content);
        string videoId = createDoc.RootElement.GetProperty("id").GetString()!;
        string status = createDoc.RootElement.GetProperty("status").GetString()!;
        int? progressPercent = TryGetProgress(createDoc.RootElement);

        progress?.Report(new VideoGenerationProgress(status, progressPercent));

        // Poll until the video generation is complete
        string? errorMessage = null;
        while (!IsTerminalStatus(status))
        {
            await Task.Delay(
                _defaultPollingInterval, cancellationToken).ConfigureAwait(false);

            var pollOpts = new RequestOptions { CancellationToken = cancellationToken };
            ClientResult getResult = await _videoClient.GetVideoAsync(
                videoId, pollOpts).ConfigureAwait(false);
            using JsonDocument statusDoc = JsonDocument.Parse(
                getResult.GetRawResponse().Content);
            status = statusDoc.RootElement.GetProperty("status").GetString()!;
            progressPercent = TryGetProgress(statusDoc.RootElement);

            progress?.Report(new VideoGenerationProgress(status, progressPercent));

            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) &&
                statusDoc.RootElement.TryGetProperty("error", out JsonElement errorEl) &&
                errorEl.TryGetProperty("message", out JsonElement msgEl))
            {
                errorMessage = msgEl.GetString();
            }
        }

        if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                errorMessage ?? "Video generation failed.");
        }

        // Honor the requested response format.
        string contentType = options?.MediaType ?? "video/mp4";
        List<AIContent> contents;

        if (options?.ResponseFormat is VideoGenerationResponseFormat.Uri or
            VideoGenerationResponseFormat.Hosted)
        {
            // Return a URI pointing to the video content endpoint without downloading
            // the potentially large video blob.
            string baseUrl = _videoClient.Endpoint.ToString().TrimEnd('/');
            var videoUri = new Uri($"{baseUrl}/videos/{videoId}/content");
            contents = [new UriContent(videoUri, contentType)];
        }
        else
        {
            // Download the completed video content.
            var dlOpts = new RequestOptions { CancellationToken = cancellationToken };
            ClientResult downloadResult = await _videoClient.DownloadVideoAsync(
                videoId, options: dlOpts).ConfigureAwait(false);
            BinaryData videoData = downloadResult.GetRawResponse().Content;
            contents = [new DataContent(videoData.ToMemory(), contentType)];
        }

        return new VideoGenerationResponse(contents);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) =>
        serviceType is null ? throw new ArgumentNullException(nameof(serviceType)) :
        serviceKey is not null ? null :
        serviceType == typeof(VideoGeneratorMetadata) ? _metadata :
        serviceType == typeof(VideoClient) ? _videoClient :
        serviceType.IsInstanceOfType(this) ? this :
        null;

    /// <inheritdoc />
    void IDisposable.Dispose()
    {
        // Nothing to dispose. Implementation required for the IVideoGenerator interface.
    }

    /// <summary>Creates a <see cref="PipelineMessage"/> for a POST request to a path not yet exposed by the SDK.</summary>
    private static PipelineMessage CreatePipelineRequest(
        VideoClient videoClient, string path, BinaryContent content,
        string contentType, RequestOptions options)
    {
        string baseUrl = videoClient.Endpoint.ToString().TrimEnd('/');
        Uri uri = new($"{baseUrl}{path}");
        PipelineMessageClassifier classifier = PipelineMessageClassifier.Create(
            stackalloc ushort[] { 200, 201, 202 });
        PipelineMessage message = videoClient.Pipeline.CreateMessage(
            uri, "POST", classifier);
        message.Request.Headers.Set("Content-Type", contentType);
        message.Request.Headers.Set("Accept", "application/json");
        message.Request.Content = content;
        message.Apply(options);
        return message;
    }

    /// <summary>Returns the string value of an additional property, or <see langword="null"/> if not present.</summary>
    private static string? GetStringAdditionalProperty(VideoGenerationOptions? options, string key) =>
        options?.AdditionalProperties is { } props &&
        props.TryGetValue(key, out object? value) &&
        value is string s ? s : null;

    /// <summary>Determines whether the given media type represents a video format.</summary>
    private static bool IsVideoMediaType(string? mediaType) =>
        mediaType is not null &&
        mediaType.StartsWith("video/", StringComparison.OrdinalIgnoreCase);

    /// <summary>Determines whether the given media type represents an image format.</summary>
    /// <remarks>Treats <see langword="null"/> or unspecified media types as images for backward compatibility.</remarks>
    private static bool IsImageMediaType(string? mediaType) =>
        mediaType is null ||
        mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);

    /// <summary>Determines whether the given key is a routing key consumed by this generator.</summary>
    private static bool IsRoutingKey(string key) =>
        string.Equals(key, EditVideoIdKey, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(key, ExtendVideoIdKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>Forwards additional properties to the JSON body, skipping routing keys.</summary>
    private static void ForwardAdditionalProperties(JsonObject body, VideoGenerationOptions? options)
    {
        if (options?.AdditionalProperties is not { } props)
        {
            return;
        }

        foreach (KeyValuePair<string, object?> prop in props)
        {
            if (!IsRoutingKey(prop.Key))
            {
                body[prop.Key] = ToJsonNode(prop.Value);
            }
        }
    }

    /// <summary>Determines whether the given status indicates the video generation job has finished.</summary>
    private static bool IsTerminalStatus(string status) =>
        string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "expired", StringComparison.OrdinalIgnoreCase);

    /// <summary>Tries to read the integer <c>progress</c> field from a video job JSON element.</summary>
    private static int? TryGetProgress(JsonElement element) =>
        element.TryGetProperty("progress", out JsonElement el) &&
        el.TryGetInt32(out int val)
            ? val
            : null;

    /// <summary>Serializes a <see cref="JsonObject"/> to UTF-8 bytes without an intermediate string allocation.</summary>
    private static BinaryData SerializeJsonToUtf8(JsonObject body)
    {
        using var ms = new MemoryStream();
        using (var writer = new Utf8JsonWriter(ms))
        {
            body.WriteTo(writer);
        }

        return new BinaryData(ms.ToArray());
    }

    /// <summary>Builds a multipart/form-data body containing the form fields and a file part.</summary>
    private static BinaryContent BuildMultipartContent(
        JsonObject formFields,
        DataContent fileContent,
        string filePartName,
        out string contentType)
    {
        string boundary = $"----MEAI{Guid.NewGuid():N}";
        contentType = $"multipart/form-data; boundary={boundary}";

        using var ms = new MemoryStream();

        foreach (KeyValuePair<string, JsonNode?> prop in formFields)
        {
            if (prop.Value is null)
            {
                continue;
            }

            string fieldValue =
                prop.Value is JsonValue jsonValue &&
                jsonValue.TryGetValue<string>(out string? stringValue)
                    ? stringValue
                    : prop.Value.ToString();

            WriteFormField(ms, boundary, prop.Key, fieldValue);
        }

        string fileName = fileContent.Name ?? filePartName;
        string mediaType =
            fileContent.MediaType ?? "application/octet-stream";
        WriteFilePart(ms, boundary, filePartName, fileName, mediaType, fileContent.Data);

        WriteString(ms, $"--{boundary}--\r\n");

        return BinaryContent.Create(new BinaryData(ms.ToArray()));
    }

    /// <summary>Writes a simple text form field to a multipart stream.</summary>
    private static void WriteFormField(
        MemoryStream ms, string boundary, string name, string value)
    {
        string header =
            $"--{boundary}\r\n" +
            $"Content-Disposition: form-data; name=\"{name}\"\r\n\r\n" +
            $"{value}\r\n";
        WriteString(ms, header);
    }

    /// <summary>Writes a file part to a multipart stream.</summary>
    private static void WriteFilePart(
        MemoryStream ms,
        string boundary,
        string name,
        string fileName,
        string mediaType,
        ReadOnlyMemory<byte> data)
    {
        string header =
            $"--{boundary}\r\n" +
            $"Content-Disposition: form-data; name=\"{name}\"; " +
            $"filename=\"{fileName}\"\r\n" +
            $"Content-Type: {mediaType}\r\n\r\n";
        WriteString(ms, header);
#if NET
        ms.Write(data.Span);
#else
        byte[] bytes = data.ToArray();
        ms.Write(bytes, 0, bytes.Length);
#endif
        WriteString(ms, "\r\n");
    }

    /// <summary>Writes a UTF-8 string to a stream.</summary>
    private static void WriteString(MemoryStream ms, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        ms.Write(bytes, 0, bytes.Length);
    }

    /// <summary>Converts an <see cref="object"/> value to a <see cref="JsonNode"/> in an AOT-safe manner.</summary>
    private static JsonNode? ToJsonNode(object? value) => value switch
    {
        null => null,
        JsonNode node => node,
        string s => JsonValue.Create(s),
        bool b => JsonValue.Create(b),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        float f => JsonValue.Create(f),
        double d => JsonValue.Create(d),
        _ => JsonValue.Create(value.ToString()!),
    };
}

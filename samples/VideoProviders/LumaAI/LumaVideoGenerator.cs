// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace LumaAI;

/// <summary>
/// Implements <see cref="IVideoGenerator"/> for the Luma AI Dream Machine API.
/// Supports text-to-video, image-to-video, extend, and keyframe interpolation.
/// </summary>
/// <remarks>
/// API Reference: https://docs.lumalabs.ai/docs/video-generation
/// Endpoint: https://api.lumalabs.ai/dream-machine/v1/generations
/// </remarks>
internal sealed class LumaVideoGenerator : IVideoGenerator
{
    private const string BaseUrl = "https://api.lumalabs.ai/dream-machine/v1";
    private readonly HttpClient _httpClient;
    private readonly string _modelId;

    public LumaVideoGenerator(string apiKey, string modelId = "ray-2", HttpClient? httpClient = null)
    {
        _modelId = modelId;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<VideoGenerationOperation> GenerateAsync(
        VideoGenerationRequest request,
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string model = options?.ModelId ?? _modelId;
        var body = new JsonObject { ["prompt"] = request.Prompt, ["model"] = model };

        // Duration
        if (options?.Duration is { } duration)
        {
            body["duration"] = $"{(int)duration.TotalSeconds}s";
        }

        // Resolution
        if (options?.VideoSize is { } size)
        {
            body["resolution"] = MapResolution(size);
        }

        // Aspect ratio — prefer first-class property, fall back to AdditionalProperties
        string? aspectRatio = options?.AspectRatio;
        if (aspectRatio is null && options?.AdditionalProperties?.TryGetValue("aspect_ratio", out object? ar) == true && ar is string arStr)
        {
            aspectRatio = arStr;
        }

        if (aspectRatio is not null)
        {
            body["aspect_ratio"] = aspectRatio;
        }

        // Loop
        if (options?.AdditionalProperties?.TryGetValue("loop", out object? loop) == true && loop is bool loopBool)
        {
            body["loop"] = loopBool;
        }

        // Concepts
        if (options?.AdditionalProperties?.TryGetValue("concepts", out object? concepts) == true && concepts is JsonArray conceptsArray)
        {
            body["concepts"] = JsonNode.Parse(conceptsArray.ToJsonString())!;
        }

        // Callback URL
        if (options?.AdditionalProperties?.TryGetValue("callback_url", out object? cbUrl) == true && cbUrl is string cbUrlStr)
        {
            body["callback_url"] = cbUrlStr;
        }

        // Build keyframes based on operation kind
        var keyframes = new JsonObject();

        switch (request.OperationKind)
        {
            case VideoOperationKind.Create:
                // Image-to-video: use original media as first frame (frame0)
                if (request.OriginalMedia is not null)
                {
                    await AddImageKeyframesAsync(keyframes, request.OriginalMedia, options);
                }

                break;

            case VideoOperationKind.Extend:
                // Extend: use SourceVideoId as frame0 generation reference
                if (request.SourceVideoId is not null)
                {
                    keyframes["frame0"] = new JsonObject
                    {
                        ["type"] = "generation",
                        ["id"] = request.SourceVideoId,
                    };
                }

                break;

            case VideoOperationKind.Edit:
                // Luma doesn't have a direct "edit" endpoint — map to video-to-video via keyframes
                if (request.SourceVideoId is not null)
                {
                    keyframes["frame0"] = new JsonObject
                    {
                        ["type"] = "generation",
                        ["id"] = request.SourceVideoId,
                    };
                }

                break;
        }

        if (keyframes.Count > 0)
        {
            body["keyframes"] = keyframes;
        }

        string json = body.ToJsonString();
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync($"{BaseUrl}/generations", content, cancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = JsonDocument.Parse(responseBody);
        string operationId = result.RootElement.GetProperty("id").GetString()!;

        return new LumaVideoGenerationOperation(operationId, _httpClient, model);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        if (serviceKey is null && serviceType.IsInstanceOfType(this))
        {
            return this;
        }

        return null;
    }

    public void Dispose() => _httpClient.Dispose();

    private static async Task AddImageKeyframesAsync(JsonObject keyframes, IEnumerable<AIContent> media, VideoGenerationOptions? options)
    {
        int index = 0;
        foreach (var item in media)
        {
            if (item is not DataContent dc)
            {
                continue;
            }

            string frameKey = index == 0 ? "frame0" : "frame1";

            if (item is UriContent uc && uc.Uri is not null)
            {
                // If it's a URL-based image, Luma requires HTTPS URLs
                keyframes[frameKey] = new JsonObject
                {
                    ["type"] = "image",
                    ["url"] = uc.Uri.ToString(),
                };
            }
            else if (dc.Data.Length > 0)
            {
                // Luma only accepts HTTPS URLs for images, not data URIs.
                // (Limitation: callers must upload images to a CDN first.)
                string dataUri = dc.Uri ?? $"data:{dc.MediaType ?? "image/png"};base64,{Convert.ToBase64String(dc.Data.ToArray())}";
                keyframes[frameKey] = new JsonObject
                {
                    ["type"] = "image",
                    ["url"] = dataUri,
                };
            }

            index++;
            if (index >= 2)
            {
                break; // Luma supports max 2 keyframes (frame0 + frame1)
            }
        }

        await Task.CompletedTask;
    }

    private static string MapResolution(Size size)
    {
        int maxDim = Math.Max(size.Width, size.Height);
        return maxDim switch
        {
            <= 540 => "540p",
            <= 720 => "720p",
            <= 1080 => "1080p",
            _ => "4k",
        };
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace GoogleVeo;

/// <summary>
/// Implements <see cref="IVideoGenerator"/> for Google Veo via the Gemini API.
/// Supports text-to-video, image-to-video, reference images, and video extension.
/// </summary>
/// <remarks>
/// API Reference: https://ai.google.dev/gemini-api/docs/video
/// Endpoint: POST https://generativelanguage.googleapis.com/v1beta/models/{model}:generateVideos
/// Polling:  GET  https://generativelanguage.googleapis.com/v1beta/{operation.name}
/// </remarks>
internal sealed class GoogleVeoVideoGenerator : IVideoGenerator
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelId;

    public GoogleVeoVideoGenerator(string apiKey, string modelId = "veo-3.1-generate-preview", HttpClient? httpClient = null)
    {
        _apiKey = apiKey;
        _modelId = modelId;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<VideoGenerationOperation> GenerateAsync(
        VideoGenerationRequest request,
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string model = options?.ModelId ?? _modelId;
        var body = new JsonObject();

        // Text prompt (required for most operations)
        if (request.Prompt is not null)
        {
            body["prompt"] = request.Prompt;
        }

        // Image for image-to-video
        if (request.OperationKind == VideoOperationKind.Create && request.OriginalMedia is not null)
        {
            var image = GetFirstImageContent(request.OriginalMedia);
            if (image is not null)
            {
                body["image"] = image;
            }
        }

        // Reference images (provider-specific via AdditionalProperties)
        if (options?.AdditionalProperties?.TryGetValue("referenceImages", out object? refImgs) == true && refImgs is JsonArray refArray)
        {
            body["referenceImages"] = JsonNode.Parse(refArray.ToJsonString())!;
        }

        // Last frame for first+last frame interpolation
        if (options?.AdditionalProperties?.TryGetValue("lastFrameImage", out object? lastFrame) == true)
        {
            body["lastFrame"] = new JsonObject
            {
                ["image"] = BuildImageNode(lastFrame),
            };
        }

        // Generation config
        var config = new JsonObject();

        if (options?.AdditionalProperties?.TryGetValue("personGeneration", out object? personGen) == true && personGen is string personGenStr)
        {
            config["personGeneration"] = personGenStr;
        }

        if (options?.Duration is { } duration)
        {
            config["durationSeconds"] = ((int)duration.TotalSeconds).ToString();
        }

        if (options?.VideoSize is { } size)
        {
            config["resolution"] = MapResolution(size);
        }

        if (options?.AspectRatio is { } aspectRatio)
        {
            config["aspectRatio"] = aspectRatio;
        }
        else if (options?.AdditionalProperties?.TryGetValue("aspectRatio", out object? ar) == true && ar is string arStr)
        {
            config["aspectRatio"] = arStr;
        }

        if (options?.AdditionalProperties?.TryGetValue("numberOfVideos", out object? numVids) == true && numVids is int numVidsInt)
        {
            config["numberOfVideos"] = numVidsInt;
        }
        else if (options?.Count is { } count)
        {
            config["numberOfVideos"] = count;
        }

        // Negative prompt — prefer first-class property on request, fall back to AdditionalProperties
        string? negativePrompt = request.NegativePrompt;
        if (negativePrompt is null && options?.AdditionalProperties?.TryGetValue("negativePrompt", out object? negPrompt) == true && negPrompt is string negPromptStr)
        {
            negativePrompt = negPromptStr;
        }

        if (negativePrompt is not null)
        {
            config["negativePrompt"] = negativePrompt;
        }

        if (options?.GenerateAudio is bool genAudio)
        {
            config["generateAudio"] = genAudio;
        }
        else if (options?.AdditionalProperties?.TryGetValue("generateAudio", out object? genAudioObj) == true && genAudioObj is bool genAudioBool)
        {
            config["generateAudio"] = genAudioBool;
        }

        if (options?.Seed is int seed)
        {
            config["seed"] = seed;
        }
        else if (options?.AdditionalProperties?.TryGetValue("seed", out object? seedObj) == true && seedObj is int seedInt)
        {
            config["seed"] = seedInt;
        }

        if (config.Count > 0)
        {
            body["generationConfig"] = config;
        }

        // Video extension uses a different field structure
        if (request.OperationKind == VideoOperationKind.Extend && request.SourceVideoId is not null)
        {
            // For extend, the sourceVideoId should be a video file URI or inline data
            // The Veo API uses the image field for the last frame of the source video
            // This is a simplification - real extension requires the Gemini Files API
            body["extensionSourceVideoId"] = request.SourceVideoId;
        }

        string url = $"{BaseUrl}/models/{model}:generateVideos?key={_apiKey}";
        string json = body.ToJsonString();
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, content, cancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = JsonDocument.Parse(responseBody);
        string operationName = result.RootElement.GetProperty("name").GetString()!;

        return new GoogleVeoVideoGenerationOperation(operationName, _apiKey, _httpClient, model);
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

    private static JsonNode? GetFirstImageContent(IEnumerable<AIContent> media)
    {
        foreach (var item in media)
        {
            if (item is DataContent dc && (dc.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false) && dc.Data.Length > 0)
            {
                return new JsonObject
                {
                    ["imageBytes"] = Convert.ToBase64String(dc.Data.ToArray()),
                    ["mimeType"] = dc.MediaType,
                };
            }

            if (item is UriContent uc && uc.Uri is not null)
            {
                return new JsonObject
                {
                    ["imageUri"] = uc.Uri.ToString(),
                };
            }
        }

        return null;
    }

    private static JsonNode BuildImageNode(object imageData)
    {
        if (imageData is string path && File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            return new JsonObject
            {
                ["imageBytes"] = Convert.ToBase64String(bytes),
                ["mimeType"] = "image/png",
            };
        }

        if (imageData is JsonNode node)
        {
            return node;
        }

        return new JsonObject { ["imageUri"] = imageData.ToString() };
    }

    private static string MapResolution(Size size)
    {
        int maxDim = Math.Max(size.Width, size.Height);
        return maxDim switch
        {
            <= 720 => "720p",
            <= 1080 => "1080p",
            _ => "4k",
        };
    }
}

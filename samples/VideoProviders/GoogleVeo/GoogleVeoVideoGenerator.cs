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
/// Endpoint: POST https://generativelanguage.googleapis.com/v1beta/models/{model}:predictLongRunning
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

        // Build instance object (prompt, image, reference images, last frame, extension)
        var instance = new JsonObject();

        // Text prompt (required for most operations)
        if (request.Prompt is not null)
        {
            instance["prompt"] = request.Prompt;
        }

        // Image for image-to-video
        if (request.OperationKind == VideoOperationKind.Create && request.StartFrame is not null)
        {
            var image = GetImageNode(request.StartFrame);
            if (image is not null)
            {
                instance["image"] = image;
            }
        }

        // Reference images (first-class property)
        if (request.ReferenceImages is { Count: > 0 } refImages)
        {
            var refArray = new JsonArray();
            foreach (var refImg in refImages)
            {
                var imgNode = BuildImageNode(refImg);
                if (imgNode is not null)
                {
                    refArray.Add(new JsonObject { ["referenceImage"] = new JsonObject { ["image"] = imgNode } });
                }
            }

            if (refArray.Count > 0)
            {
                instance["referenceImages"] = refArray;
            }
        }
        else if (options?.AdditionalProperties?.TryGetValue("referenceImages", out object? refImgs) == true && refImgs is JsonArray refArrayLegacy)
        {
            instance["referenceImages"] = JsonNode.Parse(refArrayLegacy.ToJsonString())!;
        }

        // Last frame for first+last frame interpolation
        if (request.EndFrame is not null)
        {
            instance["lastFrame"] = new JsonObject
            {
                ["image"] = BuildImageNode(request.EndFrame),
            };
        }
        else if (options?.AdditionalProperties?.TryGetValue("lastFrameImage", out object? lastFrame) == true)
        {
            instance["lastFrame"] = new JsonObject
            {
                ["image"] = BuildImageNode(lastFrame),
            };
        }

        // Video extension
        if (request.OperationKind == VideoOperationKind.Extend && request.SourceVideoId is not null)
        {
            instance["extensionSourceVideoId"] = request.SourceVideoId;
        }

        // Build the parameters object (generation config)
        var parameters = new JsonObject();

        if (options?.AdditionalProperties?.TryGetValue("personGeneration", out object? personGen) == true && personGen is string personGenStr)
        {
            parameters["personGeneration"] = personGenStr;
        }

        if (options?.Duration is { } duration)
        {
            parameters["durationSeconds"] = (int)duration.TotalSeconds;
        }

        if (options?.VideoSize is { } size)
        {
            parameters["resolution"] = MapResolution(size);
        }

        if (options?.AspectRatio is { } aspectRatio)
        {
            parameters["aspectRatio"] = aspectRatio;
        }
        else if (options?.AdditionalProperties?.TryGetValue("aspectRatio", out object? ar) == true && ar is string arStr)
        {
            parameters["aspectRatio"] = arStr;
        }

        if (options?.AdditionalProperties?.TryGetValue("numberOfVideos", out object? numVids) == true && numVids is int numVidsInt)
        {
            parameters["numberOfVideos"] = numVidsInt;
        }
        else if (options?.Count is { } count)
        {
            parameters["numberOfVideos"] = count;
        }

        // Negative prompt — prefer first-class property on request, fall back to AdditionalProperties
        string? negativePrompt = request.NegativePrompt;
        if (negativePrompt is null && options?.AdditionalProperties?.TryGetValue("negativePrompt", out object? negPrompt) == true && negPrompt is string negPromptStr)
        {
            negativePrompt = negPromptStr;
        }

        if (negativePrompt is not null)
        {
            parameters["negativePrompt"] = negativePrompt;
        }

        if (options?.GenerateAudio is bool genAudio)
        {
            parameters["generateAudio"] = genAudio;
        }
        else if (options?.AdditionalProperties?.TryGetValue("generateAudio", out object? genAudioObj) == true && genAudioObj is bool genAudioBool)
        {
            parameters["generateAudio"] = genAudioBool;
        }

        if (options?.Seed is int seed)
        {
            parameters["seed"] = seed;
        }
        else if (options?.AdditionalProperties?.TryGetValue("seed", out object? seedObj) == true && seedObj is int seedInt)
        {
            parameters["seed"] = seedInt;
        }

        // Wrap in instances/parameters envelope for predictLongRunning
        var body = new JsonObject
        {
            ["instances"] = new JsonArray { instance },
        };
        if (parameters.Count > 0)
        {
            body["parameters"] = parameters;
        }

        string url = $"{BaseUrl}/models/{model}:predictLongRunning?key={_apiKey}";
        string json = body.ToJsonString();
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync(url, content, cancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Google Veo API error {(int)response.StatusCode} ({response.StatusCode}): {responseBody}");
        }

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

    private static JsonNode? GetImageNode(AIContent content)
    {
        if (content is DataContent dc && (dc.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false) && dc.Data.Length > 0)
        {
            return new JsonObject
            {
                ["bytesBase64Encoded"] = Convert.ToBase64String(dc.Data.ToArray()),
                ["mimeType"] = dc.MediaType,
            };
        }

        if (content is UriContent uc && uc.Uri is not null)
        {
            return new JsonObject
            {
                ["gcsUri"] = uc.Uri.ToString(),
            };
        }

        return null;
    }

    private static JsonNode? BuildImageNode(object imageData)
    {
        if (imageData is AIContent aiContent)
        {
            return GetImageNode(aiContent);
        }

        if (imageData is string path && File.Exists(path))
        {
            byte[] bytes = File.ReadAllBytes(path);
            return new JsonObject
            {
                ["bytesBase64Encoded"] = Convert.ToBase64String(bytes),
                ["mimeType"] = "image/png",
            };
        }

        if (imageData is JsonNode node)
        {
            return node;
        }

        return new JsonObject { ["gcsUri"] = imageData.ToString() };
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Drawing;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;

namespace Runway;

/// <summary>
/// Implements <see cref="IVideoGenerator"/> for the Runway API.
/// Supports text-to-video, image-to-video, and video-to-video (gen4_aleph).
/// </summary>
/// <remarks>
/// API Reference: https://docs.dev.runwayml.com/api
/// Endpoints:
///   POST /v1/text_to_video
///   POST /v1/image_to_video
///   POST /v1/video_to_video
///   GET  /v1/tasks/{id}
/// </remarks>
internal sealed class RunwayVideoGenerator : IVideoGenerator
{
    private const string BaseUrl = "https://api.dev.runwayml.com";
    private const string ApiVersion = "2024-11-06";
    private readonly HttpClient _httpClient;
    private readonly string _modelId;

    public RunwayVideoGenerator(string apiKey, string modelId = "gen4_turbo", HttpClient? httpClient = null)
    {
        _modelId = modelId;
        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        _httpClient.DefaultRequestHeaders.Add("X-Runway-Version", ApiVersion);
    }

    public async Task<VideoGenerationOperation> GenerateAsync(
        VideoGenerationRequest request,
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string model = options?.ModelId ?? _modelId;
        string endpoint;
        JsonObject body;

        // Determine which endpoint to use based on operation kind and media
        if (request.OperationKind == VideoOperationKind.Edit && HasVideoMedia(request.OriginalMedia))
        {
            // Video-to-video (gen4_aleph only)
            endpoint = "/v1/video_to_video";
            body = BuildVideoToVideoBody(request, model, options);
        }
        else if (HasImageMedia(request.OriginalMedia))
        {
            // Image-to-video
            endpoint = "/v1/image_to_video";
            body = BuildImageToVideoBody(request, model, options);
        }
        else
        {
            // Text-to-video
            endpoint = "/v1/text_to_video";
            body = BuildTextToVideoBody(request, model, options);
        }

        string json = body.ToJsonString();
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var response = await _httpClient.PostAsync($"{BaseUrl}{endpoint}", content, cancellationToken);

        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = JsonDocument.Parse(responseBody);
        string taskId = result.RootElement.GetProperty("id").GetString()!;

        return new RunwayVideoGenerationOperation(taskId, _httpClient, model);
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

    private static JsonObject BuildTextToVideoBody(VideoGenerationRequest request, string model, VideoGenerationOptions? options)
    {
        var body = new JsonObject
        {
            ["model"] = model,
            ["promptText"] = request.Prompt ?? string.Empty,
            ["ratio"] = options?.AspectRatio is { } ar ? MapAspectRatio(ar) : MapRatio(options?.VideoSize),
        };

        if (options?.Duration is { } duration)
        {
            body["duration"] = (int)duration.TotalSeconds;
        }

        AddSeed(body, options);
        return body;
    }

    private static JsonObject BuildImageToVideoBody(VideoGenerationRequest request, string model, VideoGenerationOptions? options)
    {
        string? imageUri = GetFirstImageUri(request.OriginalMedia);

        var body = new JsonObject
        {
            ["model"] = model,
            ["promptText"] = request.Prompt ?? string.Empty,
            ["promptImage"] = imageUri ?? string.Empty,
            ["ratio"] = options?.AspectRatio is { } ar ? MapAspectRatio(ar) : MapRatioImageToVideo(options?.VideoSize),
        };

        if (options?.Duration is { } duration)
        {
            body["duration"] = (int)duration.TotalSeconds;
        }

        AddSeed(body, options);
        return body;
    }

    private static JsonObject BuildVideoToVideoBody(VideoGenerationRequest request, string model, VideoGenerationOptions? options)
    {
        string? videoUri = GetFirstVideoUri(request.OriginalMedia);

        var body = new JsonObject
        {
            ["model"] = "gen4_aleph", // video-to-video only supports gen4_aleph
            ["promptText"] = request.Prompt ?? string.Empty,
            ["videoUri"] = videoUri ?? string.Empty,
        };

        // Reference images for style transfer
        if (options?.AdditionalProperties?.TryGetValue("references", out object? refs) == true && refs is JsonArray refsArray)
        {
            body["references"] = JsonNode.Parse(refsArray.ToJsonString())!;
        }

        AddSeed(body, options);
        return body;
    }

    private static void AddSeed(JsonObject body, VideoGenerationOptions? options)
    {
        // Prefer first-class Seed property, fall back to AdditionalProperties
        if (options?.Seed is int seed)
        {
            body["seed"] = seed;
        }
        else if (options?.AdditionalProperties?.TryGetValue("seed", out object? seedObj) == true && seedObj is int seedInt)
        {
            body["seed"] = seedInt;
        }
    }

    private static string? GetFirstImageUri(IEnumerable<AIContent>? media)
    {
        if (media is null)
        {
            return null;
        }

        foreach (var item in media)
        {
            if (item is UriContent uc && uc.Uri is not null && (uc.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? true))
            {
                return uc.Uri.ToString();
            }

            if (item is DataContent dc && dc.Data.Length > 0 && (dc.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? true))
            {
                // Runway accepts data URIs for images
                return dc.Uri ?? $"data:{dc.MediaType ?? "image/png"};base64,{Convert.ToBase64String(dc.Data.ToArray())}";
            }
        }

        return null;
    }

    private static string? GetFirstVideoUri(IEnumerable<AIContent>? media)
    {
        if (media is null)
        {
            return null;
        }

        foreach (var item in media)
        {
            if (item is UriContent uc && uc.Uri is not null && (uc.MediaType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ?? true))
            {
                return uc.Uri.ToString();
            }

            if (item is DataContent dc && dc.Data.Length > 0 && (dc.MediaType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return dc.Uri ?? $"data:{dc.MediaType};base64,{Convert.ToBase64String(dc.Data.ToArray())}";
            }
        }

        return null;
    }

    private static bool HasImageMedia(IEnumerable<AIContent>? media)
    {
        if (media is null)
        {
            return false;
        }

        foreach (var item in media)
        {
            if (item is DataContent dc && (dc.MediaType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasVideoMedia(IEnumerable<AIContent>? media)
    {
        if (media is null)
        {
            return false;
        }

        foreach (var item in media)
        {
            if (item is DataContent dc && (dc.MediaType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ?? false))
            {
                return true;
            }
        }

        return false;
    }

    private static string MapRatio(Size? size)
    {
        if (size is null)
        {
            return "1280:720";
        }

        double ratio = (double)size.Value.Width / size.Value.Height;
        return ratio > 1.5 ? "1280:720" : "720:1280";
    }

    private static string MapRatioImageToVideo(Size? size)
    {
        if (size is null)
        {
            return "1280:720";
        }

        // Runway image-to-video supports more ratios
        double ratio = (double)size.Value.Width / size.Value.Height;
        if (ratio > 2.0)
        {
            return "1584:672";
        }

        if (ratio > 1.2)
        {
            return "1280:720";
        }

        if (ratio > 0.9)
        {
            return "960:960";
        }

        return "720:1280";
    }

    /// <summary>Maps an aspect ratio string like "16:9" to a Runway ratio string like "1280:720".</summary>
    private static string MapAspectRatio(string aspectRatio) => aspectRatio switch
    {
        "16:9" => "1280:720",
        "9:16" => "720:1280",
        "1:1" => "960:960",
        "4:3" => "1104:832",
        "3:4" => "832:1104",
        _ => aspectRatio.Replace(':', ':'), // pass through as-is if already in Runway format
    };
}

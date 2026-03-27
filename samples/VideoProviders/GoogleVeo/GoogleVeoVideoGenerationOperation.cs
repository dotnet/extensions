// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.AI;

namespace GoogleVeo;

/// <summary>
/// Tracks an in-flight Google Veo operation, polling the Gemini operations API.
/// </summary>
/// <remarks>
/// Polling: GET https://generativelanguage.googleapis.com/v1beta/{operationName}?key={apiKey}
/// Response includes "done": true when complete, with "response.generatedVideos" containing results.
/// </remarks>
internal sealed class GoogleVeoVideoGenerationOperation : VideoGenerationOperation
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private string? _status;
    private string? _failureReason;
    private bool _done;
    private readonly List<string> _videoUris = [];

    public GoogleVeoVideoGenerationOperation(string operationName, string apiKey, HttpClient httpClient, string modelId)
    {
        OperationId = operationName;
        ModelId = modelId;
        _apiKey = apiKey;
        _httpClient = httpClient;
        _status = "PROCESSING";
    }

    public override string? OperationId { get; }

    public override string? Status => _status;

    public override int? PercentComplete => _done ? 100 : null; // Veo doesn't report percent

    public override bool IsCompleted => _done;

    public override string? FailureReason => _failureReason;

    public override async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        string url = $"{BaseUrl}/{OperationId}?key={_apiKey}";
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        _done = root.TryGetProperty("done", out var doneProp) && doneProp.GetBoolean();

        if (root.TryGetProperty("error", out var error))
        {
            _status = "FAILED";
            _failureReason = error.TryGetProperty("message", out var msg) ? msg.GetString() : "Unknown error";
            _done = true;
        }
        else if (_done)
        {
            _status = "SUCCEEDED";

            // Parse generated videos
            if (root.TryGetProperty("response", out var resp) &&
                resp.TryGetProperty("generatedVideos", out var videos))
            {
                _videoUris.Clear();
                foreach (var video in videos.EnumerateArray())
                {
                    if (video.TryGetProperty("video", out var videoObj) &&
                        videoObj.TryGetProperty("uri", out var uri))
                    {
                        _videoUris.Add(uri.GetString()!);
                    }
                }
            }
        }
        else
        {
            _status = "PROCESSING";
        }
    }

    public override async Task WaitForCompletionAsync(
        IProgress<VideoGenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        while (!IsCompleted)
        {
            // Veo docs recommend ~10 second polling for video generation
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            await UpdateAsync(cancellationToken);
            progress?.Report(new VideoGenerationProgress(_status, PercentComplete));
        }

        if (_status == "FAILED")
        {
            throw new InvalidOperationException($"Video generation failed: {_failureReason}");
        }
    }

    public override async Task<IList<AIContent>> GetContentsAsync(
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsCompleted || _status == "FAILED")
        {
            throw new InvalidOperationException("The operation has not completed successfully.");
        }

        if (_videoUris.Count == 0)
        {
            await UpdateAsync(cancellationToken);
        }

        if (_videoUris.Count == 0)
        {
            throw new InvalidOperationException("No video URIs available after completion.");
        }

        var results = new List<AIContent>();
        foreach (string videoUri in _videoUris)
        {
            if (options?.ResponseFormat == VideoGenerationResponseFormat.Uri)
            {
                results.Add(new UriContent(new Uri(videoUri), "video/mp4"));
            }
            else
            {
                using var response = await _httpClient.GetAsync(videoUri, cancellationToken);
                response.EnsureSuccessStatusCode();
                byte[] data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                results.Add(new DataContent(data, "video/mp4"));
            }
        }

        return results;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.AI;

namespace LumaAI;

/// <summary>
/// Tracks an in-flight Luma AI video generation, polling GET /v1/generations/{id} for status.
/// </summary>
internal sealed class LumaVideoGenerationOperation : VideoGenerationOperation
{
    private const string BaseUrl = "https://api.lumalabs.ai/dream-machine/v1";

    private readonly HttpClient _httpClient;
    private string? _status;
    private string? _failureReason;
    private string? _videoUrl;

    public LumaVideoGenerationOperation(string operationId, HttpClient httpClient, string modelId)
    {
        OperationId = operationId;
        ModelId = modelId;
        _httpClient = httpClient;
        _status = "queued";
    }

    public override string? OperationId { get; }

    public override string? Status => _status;

    public override int? PercentComplete => _status switch
    {
        "completed" => 100,
        "failed" => null,
        "dreaming" => 50, // Luma uses "dreaming" for in-progress
        _ => 0,
    };

    public override bool IsCompleted => _status is "completed" or "failed";

    public override string? FailureReason => _failureReason;

    public override async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"{BaseUrl}/generations/{OperationId}", cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        _status = root.GetProperty("state").GetString();
        if (root.TryGetProperty("failure_reason", out var fr) && fr.ValueKind == JsonValueKind.String)
        {
            _failureReason = fr.GetString();
        }

        if (root.TryGetProperty("assets", out var assets) &&
            assets.TryGetProperty("video", out var video) &&
            video.ValueKind == JsonValueKind.String)
        {
            _videoUrl = video.GetString();
        }
    }

    public override async Task WaitForCompletionAsync(
        IProgress<VideoGenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        while (!IsCompleted)
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            await UpdateAsync(cancellationToken);
            progress?.Report(new VideoGenerationProgress(_status, PercentComplete));
        }

        if (_status == "failed")
        {
            throw new InvalidOperationException($"Video generation failed: {_failureReason}");
        }
    }

    public override async Task<IList<AIContent>> GetContentsAsync(
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsCompleted || _status == "failed")
        {
            throw new InvalidOperationException("The operation has not completed successfully.");
        }

        if (_videoUrl is null)
        {
            // Re-fetch to get the video URL
            await UpdateAsync(cancellationToken);
        }

        if (_videoUrl is null)
        {
            throw new InvalidOperationException("No video URL available after completion.");
        }

        if (options?.ResponseFormat == VideoGenerationResponseFormat.Uri)
        {
            return [new UriContent(new Uri(_videoUrl), "video/mp4")];
        }

        // Download the video data
        using var response = await _httpClient.GetAsync(_videoUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        byte[] data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return [new DataContent(data, "video/mp4")];
    }

    public override VideoGenerationRequest CreateExtensionRequest(string? prompt = null)
    {
        // Luma extend uses keyframes with type=generation, id=<this operation>
        return new VideoGenerationRequest
        {
            Prompt = prompt,
            SourceVideoId = OperationId,
            OperationKind = VideoOperationKind.Extend,
        };
    }
}

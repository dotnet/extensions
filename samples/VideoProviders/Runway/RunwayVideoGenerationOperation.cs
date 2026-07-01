// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Runway;

/// <summary>
/// Tracks an in-flight Runway task, polling GET /v1/tasks/{id} for status.
/// </summary>
/// <remarks>
/// Runway task states: PENDING, THROTTLED, RUNNING, SUCCEEDED, FAILED.
/// The API recommends polling no more than once every 5 seconds.
/// </remarks>
internal sealed class RunwayVideoGenerationOperation : VideoGenerationOperation
{
    private const string BaseUrl = "https://api.dev.runwayml.com";

    private readonly HttpClient _httpClient;
    private string? _status;
    private string? _failureReason;
    private string? _outputUrl;
    private int? _progressPercent;

    public RunwayVideoGenerationOperation(string operationId, HttpClient httpClient, string modelId)
    {
        OperationId = operationId;
        ModelId = modelId;
        _httpClient = httpClient;
        _status = "PENDING";
    }

    public override string? OperationId { get; }

    public override string? Status => _status;

    public override int? PercentComplete => _progressPercent ?? _status switch
    {
        "SUCCEEDED" => 100,
        "FAILED" => null,
        "RUNNING" => 50,
        "THROTTLED" => 10,
        _ => 0,
    };

    public override bool IsCompleted => _status is "SUCCEEDED" or "FAILED";

    public override string? FailureReason => _failureReason;

    public override async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync($"{BaseUrl}/v1/tasks/{OperationId}", cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        _status = root.GetProperty("status").GetString();

        if (root.TryGetProperty("failure", out var failure) && failure.ValueKind == JsonValueKind.String)
        {
            _failureReason = failure.GetString();
        }

        if (root.TryGetProperty("progress", out var prog) && prog.TryGetDouble(out double progressVal))
        {
            _progressPercent = (int)(progressVal * 100);
        }

        // Output can be a single URL or an array
        if (root.TryGetProperty("output", out var output))
        {
            if (output.ValueKind == JsonValueKind.Array && output.GetArrayLength() > 0)
            {
                _outputUrl = output[0].GetString();
            }
            else if (output.ValueKind == JsonValueKind.String)
            {
                _outputUrl = output.GetString();
            }
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

        if (_outputUrl is null)
        {
            await UpdateAsync(cancellationToken);
        }

        if (_outputUrl is null)
        {
            throw new InvalidOperationException("No output URL available after completion.");
        }

        if (options?.ResponseFormat == VideoGenerationResponseFormat.Uri)
        {
            return [new UriContent(new Uri(_outputUrl), "video/mp4")];
        }

        using var response = await _httpClient.GetAsync(_outputUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        byte[] data = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        return [new DataContent(data, "video/mp4")];
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using OpenAI.Videos;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an OpenAI video generation operation returned by <see cref="IVideoGenerator.GenerateAsync"/>.
/// </summary>
/// <remarks>
/// Use <see cref="OpenAIClientExtensions.UploadVideoCharacterAsync"/> to upload character assets
/// that can be referenced in subsequent video generation requests.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIOpenAIVideoClient)]
public sealed class OpenAIVideoGenerationOperation : VideoGenerationOperation
{
    /// <summary>Default polling interval for checking video generation status.</summary>
    private static readonly TimeSpan _defaultPollingInterval = TimeSpan.FromSeconds(10);

    private readonly VideoClient _videoClient;
    private string? _operationId;
    private string? _status;
    private int? _percentComplete;
    private string? _failureReason;

    /// <summary>Initializes a new instance of the <see cref="OpenAIVideoGenerationOperation"/> class.</summary>
    internal OpenAIVideoGenerationOperation(VideoClient videoClient, string operationId, string status, int? percentComplete)
    {
        _videoClient = videoClient;
        _operationId = operationId;
        _status = status;
        _percentComplete = percentComplete;
    }

    /// <inheritdoc />
    public override string? OperationId => _operationId;

    /// <inheritdoc />
    public override string? Status => _status;

    /// <inheritdoc />
    public override int? PercentComplete => _percentComplete;

    /// <inheritdoc />
    public override bool IsCompleted => IsTerminalStatus(_status);

    /// <inheritdoc />
    public override string? FailureReason => _failureReason;

    /// <inheritdoc />
    public override async Task UpdateAsync(CancellationToken cancellationToken = default)
    {
        if (_operationId is null || IsCompleted)
        {
            return;
        }

        var opts = new RequestOptions { CancellationToken = cancellationToken };
        ClientResult result = await _videoClient.GetVideoAsync(_operationId, opts).ConfigureAwait(false);
        ParseStatus(result.GetRawResponse().Content);
    }

    /// <inheritdoc />
    public override async Task WaitForCompletionAsync(
        IProgress<VideoGenerationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new VideoGenerationProgress(_status, _percentComplete));

        while (!IsCompleted)
        {
            await Task.Delay(_defaultPollingInterval, cancellationToken).ConfigureAwait(false);
            await UpdateAsync(cancellationToken).ConfigureAwait(false);
            progress?.Report(new VideoGenerationProgress(_status, _percentComplete));
        }

        if (string.Equals(_status, "failed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(_failureReason ?? "Video generation failed.");
        }
    }

    /// <inheritdoc />
    public override async Task<IList<AIContent>> GetContentsAsync(
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsCompleted)
        {
            throw new InvalidOperationException("The operation has not completed. Call WaitForCompletionAsync first.");
        }

        if (string.Equals(_status, "failed", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(_failureReason ?? "Video generation failed.");
        }

        string contentType = options?.MediaType ?? "video/mp4";

        if (options?.ResponseFormat is VideoGenerationResponseFormat.Uri or
            VideoGenerationResponseFormat.Hosted)
        {
            string baseUrl = _videoClient.Endpoint.ToString().TrimEnd('/');
            var videoUri = new Uri($"{baseUrl}/videos/{_operationId}/content");
            return [new UriContent(videoUri, contentType)];
        }

        var dlOpts = new RequestOptions { CancellationToken = cancellationToken };
        ClientResult downloadResult = await _videoClient.DownloadVideoAsync(
            _operationId!, options: dlOpts).ConfigureAwait(false);
        BinaryData videoData = downloadResult.GetRawResponse().Content;
        return [new DataContent(videoData.ToMemory(), contentType)];
    }

    private static bool IsTerminalStatus(string? status) =>
        string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(status, "expired", StringComparison.OrdinalIgnoreCase);

    /// <summary>Parses status fields from a video job JSON response.</summary>
    private void ParseStatus(BinaryData content)
    {
        using JsonDocument doc = JsonDocument.Parse(content);
        _status = doc.RootElement.GetProperty("status").GetString();

        if (doc.RootElement.TryGetProperty("progress", out JsonElement progressEl) &&
            progressEl.TryGetInt32(out int pct))
        {
            _percentComplete = pct;
        }

        if (string.Equals(_status, "failed", StringComparison.OrdinalIgnoreCase) &&
            doc.RootElement.TryGetProperty("error", out JsonElement errorEl) &&
            errorEl.TryGetProperty("message", out JsonElement msgEl))
        {
            _failureReason = msgEl.GetString();
        }
    }
}

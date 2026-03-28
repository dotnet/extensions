// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an in-flight or completed video generation operation.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="IVideoGenerator.GenerateAsync"/> is called, the provider submits a video generation
/// job and returns a <see cref="VideoGenerationOperation"/> immediately. The caller can then:
/// </para>
/// <list type="bullet">
/// <item><description>Check <see cref="Status"/> and <see cref="PercentComplete"/> for the current state.</description></item>
/// <item><description>Call <see cref="UpdateAsync"/> to poll for updated status.</description></item>
/// <item><description>Call <see cref="WaitForCompletionAsync"/> to poll until the operation reaches a terminal state.</description></item>
/// <item><description>Call <see cref="GetContentsAsync"/> to download the generated video content.</description></item>
/// <item><description>Call <see cref="CreateEditRequest"/> or <see cref="CreateExtensionRequest"/> to derive
/// follow-up requests from a completed video.</description></item>
/// </list>
/// <para>
/// Providers implement this abstract class to supply their own polling, download, and derived-request logic.
/// Provider-specific operations (e.g., character upload) can be exposed as additional public methods on
/// the concrete subclass.
/// </para>
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public abstract class VideoGenerationOperation
{
    /// <summary>Gets the provider-specific identifier for this operation.</summary>
    public abstract string? OperationId { get; }

    /// <summary>Gets the current status of the operation (e.g., "queued", "in_progress", "completed", "failed").</summary>
    public abstract string? Status { get; }

    /// <summary>Gets the completion percentage (0–100), or <see langword="null"/> if not available.</summary>
    public abstract int? PercentComplete { get; }

    /// <summary>Gets a value indicating whether the operation has reached a terminal state.</summary>
    public abstract bool IsCompleted { get; }

    /// <summary>Gets the failure reason if the operation failed, or <see langword="null"/>.</summary>
    public abstract string? FailureReason { get; }

    /// <summary>Gets or sets the model ID used for the operation.</summary>
    public string? ModelId { get; set; }

    /// <summary>Gets or sets usage details for the video generation operation.</summary>
    public UsageDetails? Usage { get; set; }

    /// <summary>Gets or sets the raw representation of the operation from an underlying implementation.</summary>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets or sets any additional properties associated with the operation.</summary>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <summary>Polls the provider for the current status of this operation.</summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the status has been refreshed.</returns>
    public abstract Task UpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>Polls the provider until the operation reaches a terminal state.</summary>
    /// <param name="progress">An optional <see cref="IProgress{T}"/> to receive progress updates during waiting.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>A task that completes when the operation has finished.</returns>
    /// <exception cref="InvalidOperationException">The operation failed.</exception>
    public abstract Task WaitForCompletionAsync(
        IProgress<VideoGenerationProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Downloads the completed video content.</summary>
    /// <param name="options">Optional options that may influence the download (e.g., <see cref="VideoGenerationOptions.MediaType"/>).</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.</param>
    /// <returns>The generated video content items.</returns>
    /// <exception cref="InvalidOperationException">The operation has not completed successfully.</exception>
    public abstract Task<IList<AIContent>> GetContentsAsync(
        VideoGenerationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a <see cref="VideoGenerationRequest"/> to edit this completed video.</summary>
    /// <param name="prompt">The prompt describing the desired edits.</param>
    /// <returns>A <see cref="VideoGenerationRequest"/> configured for editing.</returns>
    public virtual VideoGenerationRequest CreateEditRequest(string prompt)
    {
        return new VideoGenerationRequest
        {
            Prompt = prompt,
            SourceVideoId = OperationId,
            OperationKind = VideoOperationKind.Edit,
        };
    }

    /// <summary>Creates a <see cref="VideoGenerationRequest"/> to extend this completed video.</summary>
    /// <param name="prompt">An optional prompt to guide the extension.</param>
    /// <returns>A <see cref="VideoGenerationRequest"/> configured for extension.</returns>
    public virtual VideoGenerationRequest CreateExtensionRequest(string? prompt = null)
    {
        return new VideoGenerationRequest
        {
            Prompt = prompt,
            SourceVideoId = OperationId,
            OperationKind = VideoOperationKind.Extend,
        };
    }
}

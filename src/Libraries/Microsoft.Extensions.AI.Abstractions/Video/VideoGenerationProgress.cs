// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents the progress of a video generation operation.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public readonly struct VideoGenerationProgress : IEquatable<VideoGenerationProgress>
{
    /// <summary>Initializes a new instance of the <see cref="VideoGenerationProgress"/> struct.</summary>
    /// <param name="status">The current status of the video generation (e.g. "queued", "in_progress", "completed").</param>
    /// <param name="percentComplete">The completion percentage, from 0 to 100, or <see langword="null"/> if not available.</param>
    public VideoGenerationProgress(string? status, int? percentComplete)
    {
        Status = status;
        PercentComplete = percentComplete;
    }

    /// <summary>
    /// Gets the current status of the video generation (e.g. "queued", "in_progress", "completed", "failed").
    /// </summary>
    public string? Status { get; }

    /// <summary>
    /// Gets the completion percentage, from 0 to 100, or <see langword="null"/> if not available.
    /// </summary>
    public int? PercentComplete { get; }

    /// <summary>Determines whether two <see cref="VideoGenerationProgress"/> instances are equal.</summary>
    public static bool operator ==(VideoGenerationProgress left, VideoGenerationProgress right)
    {
        return left.Equals(right);
    }

    /// <summary>Determines whether two <see cref="VideoGenerationProgress"/> instances are not equal.</summary>
    public static bool operator !=(VideoGenerationProgress left, VideoGenerationProgress right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc />
    public bool Equals(VideoGenerationProgress other) =>
        string.Equals(Status, other.Status, StringComparison.Ordinal) && PercentComplete == other.PercentComplete;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is VideoGenerationProgress other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
#if NET
        return HashCode.Combine(Status, PercentComplete);
#else
        int hash = Status?.GetHashCode() ?? 0;
        return (hash * 397) ^ PercentComplete.GetHashCode();
#endif
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable S3604
// S3604: Member initializer values should not be redundant.
// We disable this warning because it is a false positive arising from the analyzer's lack of support for C#'s primary
// constructor syntax.

using System;

namespace Microsoft.Extensions.AI.Evaluation.Reporting;

/// <summary>
/// A class that records details related to a particular LLM chat conversation turn involved in the execution of a
/// particular <see cref="ScenarioRun"/>.
/// </summary>
/// <param name="latency">
/// The duration between the time when the request was sent to the LLM and the time when the response was received for
/// the chat conversation turn.
/// </param>
/// <param name="model">
/// The model that was used in the creation of the response for the chat conversation turn. Can be
/// <see langword="null"/> if this information was not available via <see cref="ChatResponse.ModelId"/>.
/// </param>
/// <param name="usage">
/// Usage details for the chat conversation turn (including input and output token counts). Can be
/// <see langword="null"/> if usage details were not available via <see cref="ChatResponse.Usage"/>.
/// </param>
/// <param name="cacheKey">
/// The cache key for the cached model response for the chat conversation turn if response caching was enabled;
/// <see langword="null"/> otherwise.
/// </param>
/// <param name="cacheHit">
/// <see langword="true"/> if response caching was enabled and the model response for the chat conversation turn was
/// retrieved from the cache; <see langword="false"/> if response caching was enabled and the model response was not
/// retrieved from the cache; <see langword="null"/> if response caching was disabled.
/// </param>
public sealed class ChatTurnDetails(
    TimeSpan latency,
    string? model = null,
    UsageDetails? usage = null,
    string? cacheKey = null,
    bool? cacheHit = null)
{
    /// <summary>
    /// Gets or sets the duration between the time when the request was sent to the LLM and the time when the response
    /// was received for the chat conversation turn.
    /// </summary>
    public TimeSpan Latency { get; set; } = latency;

    /// <summary>
    /// Gets or sets the model that was used in the creation of the response for the chat conversation turn.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> if this information was not available via <see cref="ChatResponse.ModelId"/>.
    /// </remarks>
    public string? Model { get; set; } = model;

    /// <summary>
    /// Gets or sets usage details for the chat conversation turn (including input and output token counts).
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> if usage details were not available via <see cref="ChatResponse.Usage"/>.
    /// </remarks>
    public UsageDetails? Usage { get; set; } = usage;

    /// <summary>
    /// Gets or sets the cache key for the cached model response for the chat conversation turn.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> if response caching was disabled.
    /// </remarks>
    public string? CacheKey { get; set; } = cacheKey;

    /// <summary>
    /// Gets or sets a value indicating whether the model response was retrieved from the cache.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> if response caching was disabled.
    /// </remarks>
    public bool? CacheHit { get; set; } = cacheHit;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Defines well-known status values for real-time response lifecycle messages.
/// </summary>
/// <remarks>
/// These constants represent the standard status values that may appear on
/// <see cref="ResponseCreatedRealtimeServerMessage.Status"/> when the response completes
/// (i.e., on <see cref="RealtimeServerMessageType.ResponseDone"/>).
/// Providers may use additional status values beyond those defined here.
/// </remarks>
[Experimental(DiagnosticIds.Experiments.AIRealTime, UrlFormat = DiagnosticIds.UrlFormat)]
public static class RealtimeResponseStatus
{
    /// <summary>
    /// The response completed successfully.
    /// </summary>
    public const string Completed = "completed";

    /// <summary>
    /// The response was cancelled, typically due to an interruption such as user barge-in
    /// (the user started speaking while the model was generating output).
    /// </summary>
    public const string Cancelled = "cancelled";

    /// <summary>
    /// The response ended before completing, for example because the output reached
    /// the maximum token limit.
    /// </summary>
    public const string Incomplete = "incomplete";

    /// <summary>
    /// The response failed due to an error.
    /// </summary>
    public const string Failed = "failed";
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring transcription.
/// </summary>
[Experimental(DiagnosticIds.Experiments.AISpeechToText, UrlFormat = DiagnosticIds.UrlFormat)]
public class TranscriptionOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranscriptionOptions"/> class.
    /// </summary>
    public TranscriptionOptions()
    {
    }

    /// <summary>
    /// Gets or sets the language of the input speech audio.
    /// </summary>
    /// <remarks>
    /// The language should be specified in ISO-639-1 format (e.g. "en").
    /// Supplying the input speech language improves transcription accuracy and latency.
    /// </remarks>
    public string? SpeechLanguage { get; set; }

    /// <summary>
    /// Gets or sets the model ID to use for transcription.
    /// </summary>
    public string? ModelId { get; set; }

    /// <summary>
    /// Gets or sets an optional prompt to guide the transcription.
    /// </summary>
    public string? Prompt { get; set; }
}

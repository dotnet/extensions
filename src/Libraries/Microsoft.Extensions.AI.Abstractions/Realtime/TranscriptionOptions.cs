// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents options for configuring transcription in a real-time session.
/// </summary>
[Experimental("MEAI001")]
public class TranscriptionOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranscriptionOptions"/> class.
    /// </summary>
    public TranscriptionOptions(string language, string model, string? prompt = null)
    {
        Language = language;
        Model = model;
        Prompt = prompt;
    }

    /// <summary>
    /// Gets or sets the language for transcription. The input language should be in ISO-639-1 (e.g. en).
    /// </summary>
    public string Language { get; set; }

    /// <summary>
    /// Gets or sets the model name to use for transcription.
    /// </summary>
    public string Model { get; set; }

    /// <summary>
    /// Gets or sets an optional prompt to guide the transcription.
    /// </summary>
    public string? Prompt { get; set; }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsTextToSpeechClient"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.AITextToSpeech, UrlFormat = DiagnosticIds.UrlFormat)]
public static class ConfigureOptionsTextToSpeechClientBuilderExtensions
{
    /// <summary>
    /// Adds a callback that configures a <see cref="TextToSpeechOptions"/> to be passed to the next client in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="TextToSpeechClientBuilder"/>.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="TextToSpeechOptions"/> instance.
    /// It is passed a clone of the caller-supplied <see cref="TextToSpeechOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// This method can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="TextToSpeechOptions"/> if the caller didn't supply a <see cref="TextToSpeechOptions"/> instance, or a clone (via <see cref="TextToSpeechOptions.Clone"/>)
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static TextToSpeechClientBuilder ConfigureOptions(
        this TextToSpeechClientBuilder builder, Action<TextToSpeechOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerClient => new ConfigureOptionsTextToSpeechClient(innerClient, configure));
    }
}

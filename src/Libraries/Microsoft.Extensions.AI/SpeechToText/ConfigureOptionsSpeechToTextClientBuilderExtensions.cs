// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsSpeechToTextClient"/> instances.</summary>
[Experimental("MEAI001")]
public static class ConfigureOptionsSpeechToTextClientBuilderExtensions
{
    /// <summary>
    /// Adds a callback that configures a <see cref="SpeechToTextOptions"/> to be passed to the next client in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="SpeechToTextClientBuilder"/>.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="SpeechToTextOptions"/> instance.
    /// It is passed a clone of the caller-supplied <see cref="SpeechToTextOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// This method can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="SpeechToTextOptions"/> if the caller didn't supply a <see cref="SpeechToTextOptions"/> instance, or a clone (via <see cref="SpeechToTextOptions.Clone"/>)
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static SpeechToTextClientBuilder ConfigureOptions(
        this SpeechToTextClientBuilder builder, Action<SpeechToTextOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerClient => new ConfigureOptionsSpeechToTextClient(innerClient, configure));
    }
}

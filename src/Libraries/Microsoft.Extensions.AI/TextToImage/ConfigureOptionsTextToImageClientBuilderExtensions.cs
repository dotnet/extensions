// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsTextToImageClient"/> instances.</summary>
[Experimental("MEAI001")]
public static class ConfigureOptionsTextToImageClientBuilderExtensions
{
    /// <summary>
    /// Adds a callback that configures a <see cref="TextToImageOptions"/> to be passed to the next client in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="TextToImageClientBuilder"/>.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="TextToImageOptions"/> instance.
    /// It is passed a clone of the caller-supplied <see cref="TextToImageOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="TextToImageOptions"/> if the caller didn't supply a <see cref="TextToImageOptions"/> instance, or a clone (via <see cref="TextToImageOptions.Clone"/>)
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static TextToImageClientBuilder ConfigureOptions(
        this TextToImageClientBuilder builder, Action<TextToImageOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerClient => new ConfigureOptionsTextToImageClient(innerClient, configure));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsImageGenerator"/> instances.</summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.ImageGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public static class ConfigureOptionsImageGeneratorBuilderExtensions
{
    /// <summary>
    /// Adds a callback that configures a <see cref="ImageGenerationOptions"/> to be passed to the next generator in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="ImageGeneratorBuilder"/>.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="ImageGenerationOptions"/> instance.
    /// It is passed a clone of the caller-supplied <see cref="ImageGenerationOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="ImageGenerationOptions"/> if the caller didn't supply a <see cref="ImageGenerationOptions"/> instance, or a clone (via <see cref="ImageGenerationOptions.Clone"/>)
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ImageGeneratorBuilder ConfigureOptions(
        this ImageGeneratorBuilder builder, Action<ImageGenerationOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerGenerator => new ConfigureOptionsImageGenerator(innerGenerator, configure));
    }
}

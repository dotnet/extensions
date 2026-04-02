// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsVideoGenerator"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public static class ConfigureOptionsVideoGeneratorBuilderExtensions
{
    /// <summary>
    /// Adds a callback that configures a <see cref="VideoGenerationOptions"/> to be passed to the next generator in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="VideoGeneratorBuilder"/>.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="VideoGenerationOptions"/> instance.
    /// It is passed a clone of the caller-supplied <see cref="VideoGenerationOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="VideoGenerationOptions"/> if the caller didn't supply a <see cref="VideoGenerationOptions"/> instance, or a clone (via <see cref="VideoGenerationOptions.Clone"/>)
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static VideoGeneratorBuilder ConfigureOptions(
        this VideoGeneratorBuilder builder, Action<VideoGenerationOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerGenerator => new ConfigureOptionsVideoGenerator(innerGenerator, configure));
    }
}

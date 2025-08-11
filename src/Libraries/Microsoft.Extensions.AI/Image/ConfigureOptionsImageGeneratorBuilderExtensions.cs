// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

#pragma warning disable SA1629 // Documentation text should end with a period

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsImageGenerator"/> instances.</summary>
[Experimental("MEAI001")]
public static class ConfigureOptionsImageGeneratorBuilderExtensions
{
    /// <summary>
    /// Adds a callback that configures a <see cref="ImageOptions"/> to be passed to the next generator in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="ImageGeneratorBuilder"/>.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="ImageOptions"/> instance.
    /// It is passed a clone of the caller-supplied <see cref="ImageOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="ImageOptions"/> if the caller didn't supply a <see cref="ImageOptions"/> instance, or a clone (via <see cref="ImageOptions.Clone"/>)
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static ImageGeneratorBuilder ConfigureOptions(
        this ImageGeneratorBuilder builder, Action<ImageOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerGenerator => new ConfigureOptionsImageGenerator(innerGenerator, configure));
    }
}

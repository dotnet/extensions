// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsOcrClient"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public static class ConfigureOptionsOcrClientBuilderExtensions
{
    /// <summary>
    /// Adds a callback that configures an <see cref="OcrOptions"/> to be passed to the next client in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="OcrClientBuilder"/>.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="OcrOptions"/> instance.
    /// It is passed a clone of the caller-supplied <see cref="OcrOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// This method can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="OcrOptions"/> if the caller didn't supply an <see cref="OcrOptions"/> instance, or a clone (via <see cref="OcrOptions.Clone"/>)
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static OcrClientBuilder ConfigureOptions(
        this OcrClientBuilder builder, Action<OcrOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerClient => new ConfigureOptionsOcrClient(innerClient, configure));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>Provides extensions for configuring <see cref="ConfigureOptionsDocumentExtractionClient"/> instances.</summary>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public static class ConfigureOptionsDocumentExtractionClientBuilderExtensions
{
    /// <summary>
    /// Adds a callback that configures an <see cref="DocumentExtractionOptions"/> to be passed to the next client in the pipeline.
    /// </summary>
    /// <param name="builder">The <see cref="DocumentExtractionClientBuilder"/>.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="DocumentExtractionOptions"/> instance.
    /// It is passed a clone of the caller-supplied <see cref="DocumentExtractionOptions"/> instance (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// This method can be used to set default options. The <paramref name="configure"/> delegate is passed either a new instance of
    /// <see cref="DocumentExtractionOptions"/> if the caller didn't supply an <see cref="DocumentExtractionOptions"/> instance, or a clone (via <see cref="DocumentExtractionOptions.Clone"/>)
    /// of the caller-supplied instance if one was supplied.
    /// </remarks>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static DocumentExtractionClientBuilder ConfigureOptions(
        this DocumentExtractionClientBuilder builder, Action<DocumentExtractionOptions> configure)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(configure);

        return builder.Use(innerClient => new ConfigureOptionsDocumentExtractionClient(innerClient, configure));
    }
}

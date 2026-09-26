// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>Represents a delegating OCR client that configures an <see cref="DocumentExtractionOptions"/> instance used by the remainder of the pipeline.</summary>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ConfigureOptionsDocumentExtractionClient : DelegatingDocumentExtractionClient
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Action<DocumentExtractionOptions> _configureOptions;

    /// <summary>Initializes a new instance of the <see cref="ConfigureOptionsDocumentExtractionClient"/> class with the specified <paramref name="configure"/> callback.</summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="DocumentExtractionOptions"/> instance. It is passed a clone of the caller-supplied <see cref="DocumentExtractionOptions"/> instance
    /// (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// The <paramref name="configure"/> delegate is passed either a new instance of <see cref="DocumentExtractionOptions"/> if
    /// the caller didn't supply an <see cref="DocumentExtractionOptions"/> instance, or a clone (via <see cref="DocumentExtractionOptions.Clone"/>) of the caller-supplied
    /// instance if one was supplied.
    /// </remarks>
    public ConfigureOptionsDocumentExtractionClient(IDocumentExtractionClient innerClient, Action<DocumentExtractionOptions> configure)
        : base(innerClient)
    {
        _configureOptions = Throw.IfNull(configure);
    }

    /// <inheritdoc/>
    public override async Task<DocumentExtractionResult> ExtractAsync(
        Stream document,
        string mediaType,
        DocumentExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await base.ExtractAsync(document, mediaType, Configure(options), cancellationToken);
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<DocumentExtractionPageResult> ExtractPagesAsync(
        Stream document,
        string mediaType,
        DocumentExtractionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return base.ExtractPagesAsync(document, mediaType, Configure(options), cancellationToken);
    }

    /// <summary>Creates and configures the <see cref="DocumentExtractionOptions"/> to pass along to the inner client.</summary>
    private DocumentExtractionOptions Configure(DocumentExtractionOptions? options)
    {
        options = options?.Clone() ?? new();

        _configureOptions(options);

        return options;
    }
}

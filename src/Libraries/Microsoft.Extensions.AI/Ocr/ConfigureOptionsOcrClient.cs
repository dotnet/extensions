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

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating OCR client that configures an <see cref="OcrOptions"/> instance used by the remainder of the pipeline.</summary>
[Experimental(DiagnosticIds.Experiments.AIOcr, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class ConfigureOptionsOcrClient : DelegatingOcrClient
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Action<OcrOptions> _configureOptions;

    /// <summary>Initializes a new instance of the <see cref="ConfigureOptionsOcrClient"/> class with the specified <paramref name="configure"/> callback.</summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="OcrOptions"/> instance. It is passed a clone of the caller-supplied <see cref="OcrOptions"/> instance
    /// (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <remarks>
    /// The <paramref name="configure"/> delegate is passed either a new instance of <see cref="OcrOptions"/> if
    /// the caller didn't supply an <see cref="OcrOptions"/> instance, or a clone (via <see cref="OcrOptions.Clone"/>) of the caller-supplied
    /// instance if one was supplied.
    /// </remarks>
    public ConfigureOptionsOcrClient(IOcrClient innerClient, Action<OcrOptions> configure)
        : base(innerClient)
    {
        _configureOptions = Throw.IfNull(configure);
    }

    /// <inheritdoc/>
    public override async Task<OcrResult> ExtractAsync(
        Stream document,
        string mediaType,
        OcrOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return await base.ExtractAsync(document, mediaType, Configure(options), cancellationToken);
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<OcrResponseUpdate> ExtractStreamingAsync(
        Stream document,
        string mediaType,
        OcrOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return base.ExtractStreamingAsync(document, mediaType, Configure(options), cancellationToken);
    }

    /// <summary>Creates and configures the <see cref="OcrOptions"/> to pass along to the inner client.</summary>
    private OcrOptions Configure(OcrOptions? options)
    {
        options = options?.Clone() ?? new();

        _configureOptions(options);

        return options;
    }
}

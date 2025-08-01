// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating text to image client that configures a <see cref="TextToImageOptions"/> instance used by the remainder of the pipeline.</summary>
[Experimental("MEAI001")]
public sealed class ConfigureOptionsTextToImageClient : DelegatingTextToImageClient
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Action<TextToImageOptions> _configureOptions;

    /// <summary>Initializes a new instance of the <see cref="ConfigureOptionsTextToImageClient"/> class with the specified <paramref name="configure"/> callback.</summary>
    /// <param name="innerClient">The inner client.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="TextToImageOptions"/> instance. It is passed a clone of the caller-supplied <see cref="TextToImageOptions"/> instance
    /// (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The <paramref name="configure"/> delegate is passed either a new instance of <see cref="TextToImageOptions"/> if
    /// the caller didn't supply a <see cref="TextToImageOptions"/> instance, or a clone (via <see cref="TextToImageOptions.Clone"/> of the caller-supplied
    /// instance if one was supplied.
    /// </remarks>
    public ConfigureOptionsTextToImageClient(ITextToImageClient innerClient, Action<TextToImageOptions> configure)
        : base(innerClient)
    {
        _configureOptions = Throw.IfNull(configure);
    }

    /// <inheritdoc/>
    public override async Task<TextToImageResponse> GenerateImagesAsync(
        string prompt, TextToImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await base.GenerateImagesAsync(prompt, Configure(options), cancellationToken);
    }

    /// <inheritdoc/>
    public override async Task<TextToImageResponse> EditImagesAsync(
        IEnumerable<AIContent> originalImages, string prompt, TextToImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await base.EditImagesAsync(originalImages, prompt, Configure(options), cancellationToken);
    }

    /// <summary>Creates and configures the <see cref="TextToImageOptions"/> to pass along to the inner client.</summary>
    private TextToImageOptions Configure(TextToImageOptions? options)
    {
        options = options?.Clone() ?? new();

        _configureOptions(options);

        return options;
    }
}

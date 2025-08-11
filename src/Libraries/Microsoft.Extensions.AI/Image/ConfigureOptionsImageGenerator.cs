// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Represents a delegating image generator that configures a <see cref="ImageOptions"/> instance used by the remainder of the pipeline.</summary>
[Experimental("MEAI001")]
public sealed class ConfigureOptionsImageGenerator : DelegatingImageGenerator
{
    /// <summary>The callback delegate used to configure options.</summary>
    private readonly Action<ImageOptions> _configureOptions;

    /// <summary>Initializes a new instance of the <see cref="ConfigureOptionsImageGenerator"/> class with the specified <paramref name="configure"/> callback.</summary>
    /// <param name="innerGenerator">The inner generator.</param>
    /// <param name="configure">
    /// The delegate to invoke to configure the <see cref="ImageOptions"/> instance. It is passed a clone of the caller-supplied <see cref="ImageOptions"/> instance
    /// (or a newly constructed instance if the caller-supplied instance is <see langword="null"/>).
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> or <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The <paramref name="configure"/> delegate is passed either a new instance of <see cref="ImageOptions"/> if
    /// the caller didn't supply a <see cref="ImageOptions"/> instance, or a clone (via <see cref="ImageOptions.Clone"/> of the caller-supplied
    /// instance if one was supplied.
    /// </remarks>
    public ConfigureOptionsImageGenerator(IImageGenerator innerGenerator, Action<ImageOptions> configure)
        : base(innerGenerator)
    {
        _configureOptions = Throw.IfNull(configure);
    }

    /// <inheritdoc/>
    public override async Task<ImageResponse> GenerateImagesAsync(
        ImageRequest request, ImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await base.GenerateImagesAsync(request, Configure(options), cancellationToken);
    }

    /// <inheritdoc/>
    public override IAsyncEnumerable<ImageResponseUpdate> GenerateStreamingImagesAsync(
        ImageRequest request, ImageOptions? options = null, CancellationToken cancellationToken = default)
    {
        return base.GenerateStreamingImagesAsync(request, Configure(options), cancellationToken);
    }

    /// <summary>Creates and configures the <see cref="ImageOptions"/> to pass along to the inner generator.</summary>
    private ImageOptions Configure(ImageOptions? options)
    {
        options = options?.Clone() ?? new();

        _configureOptions(options);

        return options;
    }
}

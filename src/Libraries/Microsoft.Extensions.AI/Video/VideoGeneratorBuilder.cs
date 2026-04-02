// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>A builder for creating pipelines of <see cref="IVideoGenerator"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIVideoGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public sealed class VideoGeneratorBuilder
{
    private readonly Func<IServiceProvider, IVideoGenerator> _innerGeneratorFactory;

    /// <summary>The registered generator factory instances.</summary>
    private List<Func<IVideoGenerator, IServiceProvider, IVideoGenerator>>? _generatorFactories;

    /// <summary>Initializes a new instance of the <see cref="VideoGeneratorBuilder"/> class.</summary>
    /// <param name="innerGenerator">The inner <see cref="IVideoGenerator"/> that represents the underlying backend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    public VideoGeneratorBuilder(IVideoGenerator innerGenerator)
    {
        _ = Throw.IfNull(innerGenerator);
        _innerGeneratorFactory = _ => innerGenerator;
    }

    /// <summary>Initializes a new instance of the <see cref="VideoGeneratorBuilder"/> class.</summary>
    /// <param name="innerGeneratorFactory">A callback that produces the inner <see cref="IVideoGenerator"/> that represents the underlying backend.</param>
    /// <exception cref="ArgumentNullException"><paramref name="innerGeneratorFactory"/> is <see langword="null"/>.</exception>
    public VideoGeneratorBuilder(Func<IServiceProvider, IVideoGenerator> innerGeneratorFactory)
    {
        _innerGeneratorFactory = Throw.IfNull(innerGeneratorFactory);
    }

    /// <summary>Builds an <see cref="IVideoGenerator"/> that represents the entire pipeline. Calls to this instance will pass through each of the pipeline stages in turn.</summary>
    /// <param name="services">
    /// The <see cref="IServiceProvider"/> that should provide services to the <see cref="IVideoGenerator"/> instances.
    /// If null, an empty <see cref="IServiceProvider"/> will be used.
    /// </param>
    /// <returns>An instance of <see cref="IVideoGenerator"/> that represents the entire pipeline.</returns>
    public IVideoGenerator Build(IServiceProvider? services = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var videoGenerator = _innerGeneratorFactory(services);

        // To match intuitive expectations, apply the factories in reverse order, so that the first factory added is the outermost.
        if (_generatorFactories is not null)
        {
            for (var i = _generatorFactories.Count - 1; i >= 0; i--)
            {
                videoGenerator = _generatorFactories[i](videoGenerator, services) ??
                    throw new InvalidOperationException(
                        $"The {nameof(VideoGeneratorBuilder)} entry at index {i} returned null. " +
                        $"Ensure that the callbacks passed to {nameof(Use)} return non-null {nameof(IVideoGenerator)} instances.");
            }
        }

        return videoGenerator;
    }

    /// <summary>Adds a factory for an intermediate video generator to the video generator pipeline.</summary>
    /// <param name="generatorFactory">The generator factory function.</param>
    /// <returns>The updated <see cref="VideoGeneratorBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generatorFactory"/> is <see langword="null"/>.</exception>
    public VideoGeneratorBuilder Use(Func<IVideoGenerator, IVideoGenerator> generatorFactory)
    {
        _ = Throw.IfNull(generatorFactory);

        return Use((innerGenerator, _) => generatorFactory(innerGenerator));
    }

    /// <summary>Adds a factory for an intermediate video generator to the video generator pipeline.</summary>
    /// <param name="generatorFactory">The generator factory function.</param>
    /// <returns>The updated <see cref="VideoGeneratorBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="generatorFactory"/> is <see langword="null"/>.</exception>
    public VideoGeneratorBuilder Use(Func<IVideoGenerator, IServiceProvider, IVideoGenerator> generatorFactory)
    {
        _ = Throw.IfNull(generatorFactory);

        (_generatorFactories ??= []).Add(generatorFactory);
        return this;
    }
}

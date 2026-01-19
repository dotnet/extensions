// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="IImageGenerator"/> in the context of <see cref="ImageGeneratorBuilder"/>.</summary>
[Experimental(DiagnosticIds.Experiments.AIImageGeneration, UrlFormat = DiagnosticIds.UrlFormat)]
public static class ImageGeneratorBuilderImageGeneratorExtensions
{
    /// <summary>Creates a new <see cref="ImageGeneratorBuilder"/> using <paramref name="innerGenerator"/> as its inner generator.</summary>
    /// <param name="innerGenerator">The generator to use as the inner generator.</param>
    /// <returns>The new <see cref="ImageGeneratorBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="innerGenerator"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method is equivalent to using the <see cref="ImageGeneratorBuilder"/> constructor directly,
    /// specifying <paramref name="innerGenerator"/> as the inner generator.
    /// </remarks>
    public static ImageGeneratorBuilder AsBuilder(this IImageGenerator innerGenerator)
    {
        _ = Throw.IfNull(innerGenerator);

        return new ImageGeneratorBuilder(innerGenerator);
    }
}

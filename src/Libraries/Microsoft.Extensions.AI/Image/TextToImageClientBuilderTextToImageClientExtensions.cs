// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="IImageClient"/> in the context of <see cref="ImageClientBuilder"/>.</summary>
[Experimental("MEAI001")]
public static class ImageClientBuilderImageClientExtensions
{
    /// <summary>Creates a new <see cref="ImageClientBuilder"/> using <paramref name="innerClient"/> as its inner client.</summary>
    /// <param name="innerClient">The client to use as the inner client.</param>
    /// <returns>The new <see cref="ImageClientBuilder"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="innerClient"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method is equivalent to using the <see cref="ImageClientBuilder"/> constructor directly,
    /// specifying <paramref name="innerClient"/> as the inner client.
    /// </remarks>
    public static ImageClientBuilder AsBuilder(this IImageClient innerClient)
    {
        _ = Throw.IfNull(innerClient);

        return new ImageClientBuilder(innerClient);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.AI;

/// <summary>Provides extension methods for working with <see cref="ITextToImageClient"/> in the context of <see cref="TextToImageClientBuilder"/>.</summary>
[Experimental("MEAI001")]
public static class TextToImageClientBuilderTextToImageClientExtensions
{
    /// <summary>Creates a new <see cref="TextToImageClientBuilder"/> using <paramref name="innerClient"/> as its inner client.</summary>
    /// <param name="innerClient">The client to use as the inner client.</param>
    /// <returns>The new <see cref="TextToImageClientBuilder"/> instance.</returns>
    /// <remarks>
    /// This method is equivalent to using the <see cref="TextToImageClientBuilder"/> constructor directly,
    /// specifying <paramref name="innerClient"/> as the inner client.
    /// </remarks>
    public static TextToImageClientBuilder AsBuilder(this ITextToImageClient innerClient)
    {
        _ = Throw.IfNull(innerClient);

        return new TextToImageClientBuilder(innerClient);
    }

    /// <summary>Adds the provided <see cref="ITextToImageClient"/> to the text to image client builder as the outermost client in the pipeline.</summary>
    /// <param name="builder">The <see cref="TextToImageClientBuilder"/>.</param>
    /// <param name="client">The <see cref="ITextToImageClient"/> to add to the pipeline.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static TextToImageClientBuilder Use(this TextToImageClientBuilder builder, ITextToImageClient client)
    {
        _ = Throw.IfNull(builder);
        _ = Throw.IfNull(client);

        return builder.Use(_ => client);
    }
}

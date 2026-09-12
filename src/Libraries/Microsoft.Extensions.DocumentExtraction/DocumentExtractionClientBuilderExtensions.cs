// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>Provides extension methods for working with <see cref="IDocumentExtractionClient"/> in the context of <see cref="DocumentExtractionClientBuilder"/>.</summary>
[Experimental(DiagnosticIds.Experiments.DocumentExtraction, UrlFormat = DiagnosticIds.UrlFormat)]
public static class DocumentExtractionClientBuilderExtensions
{
    /// <summary>Creates a new <see cref="DocumentExtractionClientBuilder"/> using <paramref name="innerClient"/> as its inner client.</summary>
    /// <param name="innerClient">The client to use as the inner client.</param>
    /// <returns>The new <see cref="DocumentExtractionClientBuilder"/> instance.</returns>
    /// <remarks>
    /// This method is equivalent to using the <see cref="DocumentExtractionClientBuilder"/> constructor directly,
    /// specifying <paramref name="innerClient"/> as the inner client.
    /// </remarks>
    public static DocumentExtractionClientBuilder AsBuilder(this IDocumentExtractionClient innerClient)
    {
        _ = Throw.IfNull(innerClient);

        return new DocumentExtractionClientBuilder(innerClient);
    }
}

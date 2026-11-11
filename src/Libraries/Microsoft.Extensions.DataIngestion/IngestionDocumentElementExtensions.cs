// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// Extension methods for <see cref="IngestionDocumentElement"/>.
/// </summary>
internal static class IngestionDocumentElementExtensions
{
    /// <summary>
    /// Gets the semantic content of the element if available.
    /// </summary>
    /// <param name="element">The element to get semantic content from.</param>
    /// <returns>The semantic content suitable for embedding generation.</returns>
    internal static string? GetSemanticContent(this IngestionDocumentElement element)
    {
        return element switch
        {
            IngestionDocumentImage image => image.AlternativeText ?? image.Text,
            _ => element.GetMarkdown()
        };
    }
}

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
    /// <remarks>
    /// For images, the raw markdown (e.g., <c>![Alt Text](data:image/png;base64,...)</c>) is not useful for embedding or RAG retrieval.
    /// Instead we prefer AlternativeText (a short description, usually less than 50 words) over Text (OCR result, can be several hundred words).
    /// When Address is also available, we compose a full markdown image reference so the downstream LLM can present the original image to end users.
    /// </remarks>
    internal static string? GetSemanticContent(this IngestionDocumentElement element)
    {
        if (element is IngestionDocumentImage image)
        {
            string? description = image.AlternativeText ?? image.Text;
            if (!string.IsNullOrEmpty(description) && image.Source is not null)
            {
                return $"![{description}]({image.Source.OriginalString})";
            }

            return description;
        }

        return element.GetMarkdown();
    }
}

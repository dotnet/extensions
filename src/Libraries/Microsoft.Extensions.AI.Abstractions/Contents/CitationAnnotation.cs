// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Represents an annotation that links content to source references,
/// such as documents, URLs, files, or tool outputs.
/// </summary>
public class CitationAnnotation : AIAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CitationAnnotation"/> class.
    /// </summary>
    public CitationAnnotation()
    {
    }

    /// <summary>
    /// Gets or sets the title or name of the source.
    /// </summary>
    /// <remarks>
    /// This could be the title of a document, a title from a web page, a name of a file, or similarly descriptive text.
    /// </remarks>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets a URI from which the source material was retrieved.
    /// </summary>
    public Uri? Url { get; set; }

    /// <summary>Gets or sets a source identifier associated with the annotation.</summary>
    /// <remarks>
    /// This is a provider-specific identifier that can be used to reference the source material by
    /// an ID. This may be a document ID, or a file ID, or some other identifier for the source material
    /// that can be used to uniquely identify it with the provider.
    /// </remarks>
    public string? FileId { get; set; }

    /// <summary>Gets or sets the name of any tool involved in the production of the associated content.</summary>
    /// <remarks>
    /// This might be a function name, such as one from <see cref="AITool.Name"/>, or the name of a built-in tool
    /// from the provider, such as "code_interpreter" or "file_search".
    /// </remarks>
    public string? ToolName { get; set; }

    /// <summary>
    /// Gets or sets a snippet or excerpt from the source that was cited.
    /// </summary>
    public string? Snippet { get; set; }
}

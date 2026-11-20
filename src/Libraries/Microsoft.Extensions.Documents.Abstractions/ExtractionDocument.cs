// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Documents;

/// <summary>
/// Represents an ordered, provider-neutral structured document.
/// </summary>
public sealed class ExtractionDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExtractionDocument"/> class.
    /// </summary>
    /// <param name="identifier">The local identifier of the document.</param>
    /// <param name="content">The local payload, or <see langword="null"/> for a structural document.</param>
    public ExtractionDocument(string identifier, AIContent? content = null)
    {
        Identifier = Throw.IfNullOrEmpty(identifier);
        Content = content;
    }

    /// <summary>Gets the document identifier.</summary>
    public string Identifier { get; }

    /// <summary>Gets or sets the local payload of the document, excluding its sections.</summary>
    /// <remarks>Payloads are borrowed and mutable; their interpretation is determined by the producer.</remarks>
    public AIContent? Content { get; set; }

    /// <summary>Gets the ordered root sections of the document.</summary>
    /// <remarks>
    /// Producers choose the section shape: for example, one root section per page, or nested sections that follow the
    /// document's headings. Consumers should not assume that root sections directly contain all content elements;
    /// use <see cref="EnumerateContent"/> or <see cref="GetText"/> to read content independently of the section shape.
    /// </remarks>
    public List<ExtractionDocumentSection> Sections { get; } = [];

    /// <summary>Gets the metadata associated with the document.</summary>
    public Dictionary<string, object?> Metadata { get; } = new(StringComparer.Ordinal);

    /// <summary>Gets source frames, independently of assembled section order.</summary>
    /// <remarks>Producers explicitly add frames, including blank frames with no content elements.</remarks>
    public List<ExtractionDocumentSourcePage> SourcePages { get; } = [];

    /// <summary>Gets or sets the optional borrowed native document result.</summary>
    /// <remarks>
    /// Native data is not implicitly disposed, deep-cloned, logged or serialized.
    /// This does not guarantee original response bytes or durable serialization.
    /// </remarks>
    [JsonIgnore]
    public object? RawRepresentation { get; set; }

    /// <summary>Gets all text in the document, in document order.</summary>
    /// <remarks>
    /// <para>
    /// The text is read depth-first in pre-order: a node's local <see cref="TextContent"/> payload is read before its
    /// children. The document's local payload is read first, followed by its sections. Each section's local payload
    /// is followed by its elements. Each table's local payload is followed by the content of each anchor cell in
    /// row-major order. Each image's local <see cref="TextContent"/> payload is followed by
    /// <see cref="ExtractionDocumentImageElement.Text"/>.
    /// </para>
    /// <para>
    /// Only <see cref="TextContent"/> payloads and image text are read. Other payloads such as bytes or references,
    /// <see cref="ExtractionDocumentImageElement.AlternativeText"/>, metadata and native representations are excluded.
    /// Elements are not filtered by role, so page headers and footers are included. Text is returned verbatim, without
    /// converting formatted text, and non-empty text blocks are separated by <see cref="Environment.NewLine"/>.
    /// </para>
    /// <para>
    /// Shared acyclic content is read at every occurrence. Cyclic section or table relationships and null entries are
    /// rejected.
    /// </para>
    /// </remarks>
    /// <returns>The text in the document, or an empty string when the document has no text.</returns>
    /// <exception cref="InvalidOperationException">The document contains a null entry or a cyclic relationship.</exception>
    public string GetText() => ExtractionDocumentText.Read(this);

    /// <summary>Enumerates non-section content in depth-first document order.</summary>
    /// <remarks>
    /// <para>
    /// This structural traversal descends through nested sections and yields every non-section element, such as text,
    /// image, table and custom elements. It does not yield sections themselves, does not descend into table cells,
    /// and does not return the document's or sections' local <see cref="Content"/> payloads.
    /// </para>
    /// <para>
    /// Text can therefore exist outside the yielded text elements: in a section's local payload, in table cells
    /// (whose content can itself be a section), in <see cref="ExtractionDocumentImageElement.Text"/>, and in this
    /// document's local payload. Use <see cref="GetText"/> to read all of that text, or inspect the hierarchy and
    /// <see cref="ExtractionDocumentTableElement.Cells"/> directly.
    /// </para>
    /// <para>Cyclic section relationships and null entries are rejected when traversed.</para>
    /// </remarks>
    /// <returns>The content elements in document order.</returns>
    public IEnumerable<ExtractionDocumentElement> EnumerateContent()
    {
        HashSet<ExtractionDocumentSection> path = [];
        foreach (ExtractionDocumentSection section in Sections)
        {
            foreach (ExtractionDocumentElement element in EnumerateSection(section, path))
            {
                yield return element;
            }
        }
    }

    internal static IEnumerable<ExtractionDocumentElement> EnumerateSection(
        ExtractionDocumentSection section, HashSet<ExtractionDocumentSection> path)
    {
        if (section is null || !path.Add(section))
        {
            throw new InvalidOperationException("A document cannot traverse a null section or a cyclic section relationship.");
        }

        try
        {
            foreach (ExtractionDocumentElement element in section.Elements)
            {
                if (element is null)
                {
                    throw new InvalidOperationException("A document cannot traverse a null element.");
                }

                if (element is ExtractionDocumentSection nestedSection)
                {
                    foreach (ExtractionDocumentElement nestedElement in EnumerateSection(nestedSection, path))
                    {
                        yield return nestedElement;
                    }
                }
                else
                {
                    yield return element;
                }
            }
        }
        finally
        {
            _ = path.Remove(section);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DataIngestion;

/// <summary>
/// A format-agnostic container that normalizes diverse input formats into a structured hierarchy.
/// </summary>
public sealed class IngestionDocument
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IngestionDocument"/> class.
    /// </summary>
    /// <param name="identifier">The unique identifier for the document.</param>
    /// <exception cref="ArgumentNullException"><paramref name="identifier"/> is <see langword="null"/>.</exception>
    public IngestionDocument(string identifier)
    {
        Identifier = Throw.IfNullOrEmpty(identifier);
    }

    /// <summary>
    /// Gets the unique identifier for the document.
    /// </summary>
    public string Identifier { get; }

    /// <summary>
    /// Gets the sections of the document.
    /// </summary>
    public IList<IngestionDocumentSection> Sections { get; } = [];

    /// <summary>
    /// Iterate over all elements in the document, including those in nested sections.
    /// </summary>
    /// <returns>An enumerable collection of elements.</returns>
    /// <remarks>
    /// Sections themselves are not included.
    /// </remarks>
    public IEnumerable<IngestionDocumentElement> EnumerateContent()
    {
        Stack<IngestionDocumentElement> elementsToProcess = new();

        for (int sectionIndex = Sections.Count - 1; sectionIndex >= 0; sectionIndex--)
        {
            elementsToProcess.Push(Sections[sectionIndex]);
        }

        while (elementsToProcess.Count > 0)
        {
            IngestionDocumentElement currentElement = elementsToProcess.Pop();

            if (currentElement is not IngestionDocumentSection nestedSection)
            {
                yield return currentElement;
            }
            else
            {
                for (int i = nestedSection.Elements.Count - 1; i >= 0; i--)
                {
                    elementsToProcess.Push(nestedSection.Elements[i]);
                }
            }
        }
    }
}

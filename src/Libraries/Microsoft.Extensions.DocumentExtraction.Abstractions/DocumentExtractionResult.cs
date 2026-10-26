// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Documents;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Represents the finalized result of one streaming extraction.
/// </summary>
public sealed class DocumentExtractionResult : IDisposable
{
    private bool _disposed;

    internal DocumentExtractionResult(Document document, IReadOnlyList<DocumentExtractionPage> pages)
    {
        Document = Throw.IfNull(document);
        Pages = new ReadOnlyCollection<DocumentExtractionPage>(new List<DocumentExtractionPage>(Throw.IfNull(pages)));
    }

    /// <summary>Gets the canonical Documents document.</summary>
    public Document Document { get; }

    /// <summary>Gets the finalized pages in ascending page-number order.</summary>
    public IReadOnlyList<DocumentExtractionPage> Pages { get; }

    /// <summary>Gets a value indicating whether this result has been disposed.</summary>
    internal bool IsDisposed => _disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Document.Dispose();
        }
    }
}

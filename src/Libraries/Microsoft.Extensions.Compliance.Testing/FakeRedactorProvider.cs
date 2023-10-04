// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// A provider of fake redactors.
/// </summary>
public class FakeRedactorProvider : IRedactorProvider
{
    private readonly FakeRedactor _redactor;
    private int _redactorsRequestedSoFar;

    /// <summary>
    /// Gets the collector that stores data about usage of fake redaction classes.
    /// </summary>
    public FakeRedactionCollector Collector { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FakeRedactorProvider"/> class.
    /// </summary>
    /// <param name="options">Fake redactor options.</param>
    /// <param name="eventCollector">Collects information about redactor requests.</param>
    public FakeRedactorProvider(FakeRedactorOptions? options = null, FakeRedactionCollector? eventCollector = null)
    {
        Collector = eventCollector ?? new FakeRedactionCollector();
        _redactor = new FakeRedactor(Microsoft.Extensions.Options.Options.Create(options ?? new FakeRedactorOptions()), Collector);
    }

    /// <inheritdoc/>
    public Redactor GetRedactor(DataClassification classification)
    {
        var order = Interlocked.Increment(ref _redactorsRequestedSoFar);

        Collector.Append(new RedactorRequested(classification, order));

        return _redactor;
    }

    /// <inheritdoc/>
    [Experimental(diagnosticId: Experiments.Compliance)]
    public Redactor GetRedactor(IReadOnlyList<DataClassification> classifications)
    {
        var order = Interlocked.Increment(ref _redactorsRequestedSoFar);

        Collector.Append(new RedactorRequested(classifications, order));

        return _redactor;
    }
}

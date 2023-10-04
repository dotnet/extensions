// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// A provider that only returns the <see langword="null"/> redactor implementation used for situations that don't require redaction.
/// </summary>
public sealed class NullRedactorProvider : IRedactorProvider
{
    /// <summary>
    /// Gets the singleton instance of this class.
    /// </summary>
    public static NullRedactorProvider Instance { get; } = new();

    /// <inheritdoc/>
    public Redactor GetRedactor(DataClassification classification) => NullRedactor.Instance;

    /// <inheritdoc/>
    [Experimental(diagnosticId: Experiments.Compliance)]
    public Redactor GetRedactor(IReadOnlySet<DataClassification> classifications) => NullRedactor.Instance;
}

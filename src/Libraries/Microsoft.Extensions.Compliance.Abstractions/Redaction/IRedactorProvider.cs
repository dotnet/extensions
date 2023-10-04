// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Provides redactors for different data classes.
/// </summary>
public interface IRedactorProvider
{
    /// <summary>
    /// Gets the redactor configured to handle the specified data class.
    /// </summary>
    /// <param name="classification">Data classification of the data to redact.</param>
    /// <returns>A redactor suitable to redact data of the given class.</returns>
    Redactor GetRedactor(DataClassification classification);

    /// <summary>
    /// Gets the redactor configured to handle the specified data classes.
    /// </summary>
    /// <param name="classifications">Data classifications of the data to redact.</param>
    /// <returns>A redactor suitable to redact data of the given classes.</returns>
    [Experimental(diagnosticId: Experiments.Compliance)]
    Redactor GetRedactor(IReadOnlyList<DataClassification> classifications);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Compliance.Classification;

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
}

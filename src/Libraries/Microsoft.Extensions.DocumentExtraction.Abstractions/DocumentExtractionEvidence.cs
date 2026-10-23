// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Documents;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.DocumentExtraction;

/// <summary>
/// Associates one extraction observation with an opaque source-evidence reference.
/// </summary>
/// <remarks>
/// This contract deliberately contains no provider payload, credentials, geometry, or confidence value.
/// The reference remains an association only; it is not interpreted as grounding by this package.
/// </remarks>
public sealed class DocumentExtractionEvidence
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentExtractionEvidence"/> class.
    /// </summary>
    /// <param name="reference">The opaque evidence reference.</param>
    /// <param name="role">An optional opaque role for the reference.</param>
    public DocumentExtractionEvidence(DocumentEvidenceReference reference, string? role = null)
    {
        Reference = Throw.IfNull(reference);
        Role = role;
    }

    /// <summary>
    /// Gets the opaque evidence reference.
    /// </summary>
    public DocumentEvidenceReference Reference { get; }

    /// <summary>
    /// Gets the optional opaque role.
    /// </summary>
    public string? Role { get; }
}

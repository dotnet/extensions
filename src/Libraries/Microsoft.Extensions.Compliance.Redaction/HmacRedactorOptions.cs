// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// A redactor using HMACSHA256 to encode data being redacted.
/// </summary>
[Experimental(diagnosticId: DiagnosticIds.Experiments.Compliance, UrlFormat = DiagnosticIds.UrlFormat)]
public class HmacRedactorOptions
{
    /// <summary>
    /// Gets or sets the key ID.
    /// </summary>
    /// <value>
    /// Default set to <see langword="null"/>.
    /// </value>
    /// <remarks>
    /// The key id is appended to each redacted value and is intended to identity the key that was used to hash the data.
    /// In general, every distinct key should have a unique id associated with it. When the hashed values have different key ids,
    /// it means the values are unrelated and can't be used for correlation.
    /// </remarks>
    public int? KeyId { get; set; }

    /// <summary>
    /// Gets or sets the hashing key.
    /// </summary>
    /// <remarks>
    /// The key is specified in base 64 format, and must be a minimum of 44 characters long.
    ///
    /// We recommend using a distinct key for each major deployment of a service (say for each region the service is in). Additionally,
    /// the key material should be kept secret, and rotated on a regular basis.
    /// </remarks>
    /// <value>
    /// Default set to <see cref="string.Empty" />.
    /// </value>
    [StringSyntax("Base64")]
#if NET8_0_OR_GREATER
    [System.ComponentModel.DataAnnotations.Base64String]
#endif
    [Microsoft.Shared.Data.Validation.Length(44)]
    public string Key { get; set; } = string.Empty;
}

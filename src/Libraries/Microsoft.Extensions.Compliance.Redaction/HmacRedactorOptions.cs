// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Compliance.Redaction;

/// <summary>
/// Office redactor redactorOptions.
/// </summary>
[Experimental(diagnosticId: Experiments.Compliance, UrlFormat = Experiments.UrlFormat)]
public class HmacRedactorOptions
{
    /// <summary>
    /// Gets or sets key ID.
    /// </summary>
    /// <remarks>
    /// Default set to 0.
    /// </remarks>
    public int KeyId { get; set; }

    /// <summary>
    /// Gets or sets hashing key.
    /// </summary>
    /// <remarks>
    /// It should be provided in base64 format.
    /// It should be at least 32 characters long.
    /// Default set to <see cref="string.Empty" />.
    /// </remarks>
    [StringSyntax("Base64")]
    [Microsoft.Shared.Data.Validation.Length(32)]
    public string Key { get; set; } = string.Empty;
}

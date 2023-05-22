// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Compliance.Testing;

/// <summary>
/// Options to control the fake redactor.
/// </summary>
public class FakeRedactorOptions
{
    internal const string DefaultFormat = "{0}";

    /// <summary>
    /// Gets or sets a value indicating how to format redacted data.
    /// </summary>
    /// <remarks>
    /// This is a composite format string that determines how redacted data looks like.
    /// Defaults to {0}.
    /// </remarks>
    [Required]
    [StringSyntax(StringSyntaxAttribute.CompositeFormat)]
    public string RedactionFormat { get; set; } = DefaultFormat;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Custom Result chaos policy options definition.
/// </summary>
[Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class CustomResultPolicyOptions : ChaosPolicyOptionsBase
{
    /// <summary>
    /// Gets or sets the custom result key.
    /// </summary>
    /// <remarks>
    /// This key is used for fetching the custom defined result object
    /// from <see cref="ICustomResultRegistry"/>.
    /// Default is set to <see cref="string.Empty"/>.
    /// </remarks>
    [Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    [Required]
    public string CustomResultKey { get; set; } = string.Empty;
}

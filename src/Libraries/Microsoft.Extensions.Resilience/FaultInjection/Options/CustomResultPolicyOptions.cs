// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Custom Result chaos policy options definition.
/// </summary>
[Experimental(diagnosticId: Experiments.Resilience, UrlFormat = Experiments.UrlFormat)]
public class CustomResultPolicyOptions : ChaosPolicyOptionsBase
{
    /// <summary>
    /// Gets or sets the custom result key.
    /// </summary>
    /// <value>
    /// The default is <see cref="string.Empty"/>.
    /// </value>
    /// <remarks>
    /// This key is used for fetching the custom defined result object
    /// from <see cref="ICustomResultRegistry"/>.
    /// </remarks>
    [Experimental(diagnosticId: Experiments.Resilience, UrlFormat = Experiments.UrlFormat)]
    [Required]
    public string CustomResultKey { get; set; } = string.Empty;
}

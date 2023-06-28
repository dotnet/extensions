﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Custom Result chaos policy options definition.
/// </summary>
[Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
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
    [Experimental(diagnosticId: "TBD", UrlFormat = "TBD")]
    [Required]
    public string CustomResultKey { get; set; } = string.Empty;
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Class for the fallback options definition.
/// </summary>
[Experimental(HttpClientBuilderExtensions.FallbackExperimentalMessage)]
public class FallbackClientHandlerOptions
{
    /// <summary>
    /// Gets or sets the base fallback URI.
    /// </summary>
    [Required]
    public Uri? BaseFallbackUri { get; set; }

    /// <summary>
    /// Gets or sets the fallback policy options.
    /// </summary>
    [ValidateObjectMembers]
    public HttpFallbackPolicyOptions FallbackPolicyOptions { get; set; } = new();
}

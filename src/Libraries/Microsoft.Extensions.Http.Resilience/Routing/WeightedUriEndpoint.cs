// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents an URI based endpoint with a weight assigned.
/// </summary>
public class WeightedUriEndpoint
{
    private const int MinWeight = 1;
    private const int MaxWeight = 64000;
    private const int DefaultWeight = MaxWeight / 2;

    /// <summary>
    /// Gets or sets the URL of the endpoint.
    /// </summary>
    /// <remarks>
    /// Only schema, domain name and, port is used, rest of the URL is constructed from request URL.
    /// </remarks>
    [Required]
    public Uri? Uri { get; set; }

    /// <summary>
    /// Gets or sets the weight of the endpoint.
    /// </summary>
    /// <remarks>
    /// Default value is 32000.
    /// </remarks>
    [Range(MinWeight, MaxWeight)]
    public int Weight { get; set; } = DefaultWeight;
}

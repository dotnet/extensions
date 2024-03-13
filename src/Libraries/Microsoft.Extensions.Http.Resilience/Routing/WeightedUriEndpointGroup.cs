// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents a collection of <see cref="UriEndpoint"/> with a weight assigned.
/// </summary>
public class WeightedUriEndpointGroup : UriEndpointGroup
{
    private const int MinWeight = 1;
    private const int MaxWeight = 64000;
    private const int DefaultWeight = MaxWeight / 2;

    /// <summary>
    /// Gets or sets the weight of the group.
    /// </summary>
    /// <value>
    /// The default is 32000.
    /// </value>
    [Range(MinWeight, MaxWeight)]
    public int Weight { get; set; } = DefaultWeight;
}


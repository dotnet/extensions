// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options.Validation;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Class for chaos policy options group.
/// </summary>
public class ChaosPolicyOptionsGroup
{
    /// <summary>
    /// Gets or sets the latency policy options of the chaos policy options group.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="null"/>.
    /// </remarks>
    [ValidateObjectMembers]
    public LatencyPolicyOptions? LatencyPolicyOptions { get; set; }

    /// <summary>
    /// Gets or sets the http response injection policy options of the chaos policy options group.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="null"/>.
    /// </remarks>
    [ValidateObjectMembers]
    public HttpResponseInjectionPolicyOptions? HttpResponseInjectionPolicyOptions { get; set; }

    /// <summary>
    /// Gets or sets the exception policy options of the chaos policy options group.
    /// </summary>
    /// <remarks>
    /// Default set to <see langword="null"/>.
    /// </remarks>
    [ValidateObjectMembers]
    public ExceptionPolicyOptions? ExceptionPolicyOptions { get; set; }

    /// <summary>
    /// Gets or sets the custom result policy options of the chaos policy options group.
    /// </summary>
    [ValidateObjectMembers]
    [Experimental]
    public CustomResultPolicyOptions? CustomResultPolicyOptions { get; set; }
}

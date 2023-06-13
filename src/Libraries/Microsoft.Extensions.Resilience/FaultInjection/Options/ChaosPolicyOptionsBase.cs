// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Chaos policy options base class.
/// </summary>
public class ChaosPolicyOptionsBase
{
    internal const double DefaultInjectionRate = 0.1;
    internal const bool DefaultEnabled = false;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaosPolicyOptionsBase"/> class.
    /// </summary>
    protected ChaosPolicyOptionsBase()
    {
    }

    /// <summary>
    /// Gets or sets a value indicating whether
    /// a chaos policy should be enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool Enabled { get; set; } = DefaultEnabled;

    /// <summary>
    /// Gets or sets the injection rate.
    /// </summary>
    /// <remarks>
    /// The value should be a decimal between 0 and 1 inclusive,
    /// and it indicates the rate at which a chaos policy injects faults.
    /// 0 indicates an injection rate of 0% while 1 indicates an injection rate of 100%.
    /// Default is set to 0.1.
    /// </remarks>
    [Range(0, 1)]
    public double FaultInjectionRate { get; set; } = DefaultInjectionRate;
}

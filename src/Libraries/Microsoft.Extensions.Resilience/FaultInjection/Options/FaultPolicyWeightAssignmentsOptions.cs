// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Shared.DiagnosticIds;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Contains fault-injection policy weight assignments.
/// </summary>
[Experimental(diagnosticId: Experiments.Resilience, UrlFormat = Experiments.UrlFormat)]
public class FaultPolicyWeightAssignmentsOptions
{
    /// <summary>
    /// Gets or sets the dictionary that defines fault policy weight assignments.
    /// </summary>
    /// <remarks>
    /// The key of an entry is the identifier name of a chaos policy, while the value of an entry is the weight value for the chaos policy.
    /// The weight value ranges from 0 to 100. 0 translates to 0%, and 100 translates to 100%. The total weight should add up to 100.
    /// </remarks>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Options pattern.")]
    public IDictionary<string, double> WeightAssignments { get; set; } = new Dictionary<string, double>();
}

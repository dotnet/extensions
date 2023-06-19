// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Class to contain fault-injection policy weight assignments.
/// </summary>
[Experimental(diagnosticId: "NETEXT0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class FaultPolicyWeightAssignmentsOptions
{
    /// <summary>
    /// Gets or sets the dictionary that defines fault policy weight assignments.
    /// </summary>
    /// <remarks>
    /// The key of an entry shall be the identifier name of a chaos policy, while the value of an entry shall be the weight value for the chaos policy.
    /// The weight value ranges from 0 to 100, with 0 translates to 0% while 100 translates to 100%. And the total weight shall add up to 100.
    /// </remarks>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Options pattern.")]
    public IDictionary<string, double> WeightAssignments { get; set; } = new Dictionary<string, double>();
}

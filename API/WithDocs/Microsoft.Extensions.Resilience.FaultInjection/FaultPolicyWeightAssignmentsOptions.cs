// Assembly 'Microsoft.Extensions.Resilience'

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Contains fault-injection policy weight assignments.
/// </summary>
[Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class FaultPolicyWeightAssignmentsOptions
{
    /// <summary>
    /// Gets or sets the dictionary that defines fault policy weight assignments.
    /// </summary>
    /// <remarks>
    /// The key of an entry is the identifier name of a chaos policy, while the value of an entry is the weight value for the chaos policy.
    /// The weight value ranges from 0 to 100. 0 translates to 0%, and 100 translates to 100%. The total weight should add up to 100.
    /// </remarks>
    public IDictionary<string, double> WeightAssignments { get; set; }

    public FaultPolicyWeightAssignmentsOptions();
}

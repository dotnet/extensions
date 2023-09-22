// Assembly 'Microsoft.Extensions.Resilience'

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience.FaultInjection;

[Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class FaultPolicyWeightAssignmentsOptions
{
    public IDictionary<string, double> WeightAssignments { get; set; }
    public FaultPolicyWeightAssignmentsOptions();
}

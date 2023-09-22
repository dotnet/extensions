// Assembly 'Microsoft.Extensions.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience.FaultInjection;

[Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class CustomResultPolicyOptions : ChaosPolicyOptionsBase
{
    [Required]
    public string CustomResultKey { get; set; }
    public CustomResultPolicyOptions();
}

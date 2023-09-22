// Assembly 'Microsoft.Extensions.Resilience'

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Resilience.FaultInjection;

public class ChaosPolicyOptionsGroup
{
    [ValidateObjectMembers]
    public LatencyPolicyOptions? LatencyPolicyOptions { get; set; }
    [ValidateObjectMembers]
    public HttpResponseInjectionPolicyOptions? HttpResponseInjectionPolicyOptions { get; set; }
    [ValidateObjectMembers]
    public ExceptionPolicyOptions? ExceptionPolicyOptions { get; set; }
    [ValidateObjectMembers]
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public CustomResultPolicyOptions? CustomResultPolicyOptions { get; set; }
    public ChaosPolicyOptionsGroup();
}

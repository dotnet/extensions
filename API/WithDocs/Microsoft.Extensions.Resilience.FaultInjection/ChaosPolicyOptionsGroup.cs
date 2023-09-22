// Assembly 'Microsoft.Extensions.Resilience'

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Class for chaos policy options group.
/// </summary>
public class ChaosPolicyOptionsGroup
{
    /// <summary>
    /// Gets or sets the latency policy options of the chaos policy options group.
    /// </summary>
    /// <value>
    /// The default value is <see langword="null" />.
    /// </value>
    [ValidateObjectMembers]
    public LatencyPolicyOptions? LatencyPolicyOptions { get; set; }

    /// <summary>
    /// Gets or sets the http response injection policy options of the chaos policy options group.
    /// </summary>
    /// <value>
    /// The default value is <see langword="null" />.
    /// </value>
    [ValidateObjectMembers]
    public HttpResponseInjectionPolicyOptions? HttpResponseInjectionPolicyOptions { get; set; }

    /// <summary>
    /// Gets or sets the exception policy options of the chaos policy options group.
    /// </summary>
    /// <value>
    /// The default value is <see langword="null" />.
    /// </value>
    [ValidateObjectMembers]
    public ExceptionPolicyOptions? ExceptionPolicyOptions { get; set; }

    /// <summary>
    /// Gets or sets the custom result policy options of the chaos policy options group.
    /// </summary>
    [ValidateObjectMembers]
    [Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public CustomResultPolicyOptions? CustomResultPolicyOptions { get; set; }

    public ChaosPolicyOptionsGroup();
}

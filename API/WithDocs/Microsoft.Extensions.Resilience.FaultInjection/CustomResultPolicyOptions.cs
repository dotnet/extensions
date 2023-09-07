// Assembly 'Microsoft.Extensions.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Custom Result chaos policy options definition.
/// </summary>
[Experimental("EXTEXP0001", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class CustomResultPolicyOptions : ChaosPolicyOptionsBase
{
    /// <summary>
    /// Gets or sets the custom result key.
    /// </summary>
    /// <value>
    /// The default is <see cref="F:System.String.Empty" />.
    /// </value>
    /// <remarks>
    /// This key is used for fetching the custom defined result object
    /// from <see cref="T:Microsoft.Extensions.Resilience.FaultInjection.ICustomResultRegistry" />.
    /// </remarks>
    [Required]
    public string CustomResultKey { get; set; }

    public CustomResultPolicyOptions();
}

// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents an URI based endpoint with a weight assigned.
/// </summary>
public class WeightedEndpoint
{
    /// <summary>
    /// Gets or sets the URL of the endpoint.
    /// </summary>
    /// <remarks>
    /// Only schema, domain name and, port is used, rest of the URL is constructed from request URL.
    /// </remarks>
    [Required]
    public Uri? Uri { get; set; }

    /// <summary>
    /// Gets or sets the weight of the endpoint.
    /// </summary>
    /// <remarks>
    /// Default value is 32000.
    /// </remarks>
    [Range(1, 64000)]
    public int Weight { get; set; }

    public WeightedEndpoint();
}

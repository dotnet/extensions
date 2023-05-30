// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents an URI based endpoint.
/// </summary>
public class Endpoint
{
    /// <summary>
    /// Gets or sets the URL of the endpoint.
    /// </summary>
    /// <remarks>
    /// Only schema, domain name and, port will be used, rest of the URL is constructed from request URL.
    /// </remarks>
    [Required]
    public Uri? Uri { get; set; }

    public Endpoint();
}

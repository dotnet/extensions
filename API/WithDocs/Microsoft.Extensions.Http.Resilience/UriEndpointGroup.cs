// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents a collection of <see cref="T:Microsoft.Extensions.Http.Resilience.WeightedUriEndpoint" />.
/// </summary>
public class UriEndpointGroup
{
    /// <summary>
    /// Gets or sets the endpoints in this endpoint group.
    /// </summary>
    /// <remarks>
    /// By default the endpoints are initialized with an empty list.
    /// The client must define the endpoint for each endpoint group.
    /// At least one endpoint must be defined on each endpoint group in order to performed hedged requests.
    /// </remarks>
    [Length(1, int.MaxValue)]
    [ValidateEnumeratedItems]
    public IList<WeightedUriEndpoint> Endpoints { get; set; }

    public UriEndpointGroup();
}

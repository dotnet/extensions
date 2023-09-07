// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents a collection of <see cref="T:Microsoft.Extensions.Http.Resilience.WeightedEndpoint" />.
/// </summary>
public class EndpointGroup
{
    /// <summary>
    /// Gets or sets the endpoints in this endpoint group.
    /// </summary>
    /// <remarks>
    /// By default the endpoints are initialized with an empty list.
    /// The client must define the endpoint for each endpoint group.
    /// At least one endpoint must be defined on each endpoint group in order to performed hedged requests.
    /// </remarks>
    [Microsoft.Shared.Data.Validation.Length(1)]
    [ValidateEnumeratedItems]
    public IList<WeightedEndpoint> Endpoints { get; set; }

    public EndpointGroup();
}

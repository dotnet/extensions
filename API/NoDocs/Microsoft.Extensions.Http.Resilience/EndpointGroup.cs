// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

public class EndpointGroup
{
    [Length(1, int.MaxValue)]
    [ValidateEnumeratedItems]
    public IList<WeightedEndpoint> Endpoints { get; set; }
    public EndpointGroup();
}

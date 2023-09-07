// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Http.Resilience;

public class EndpointGroup
{
    [Microsoft.Shared.Data.Validation.Length(1)]
    [ValidateEnumeratedItems]
    public IList<WeightedEndpoint> Endpoints { get; set; }
    public EndpointGroup();
}

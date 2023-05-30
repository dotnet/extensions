// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.Resilience;

public class WeightedEndpointGroup : EndpointGroup
{
    [Range(1, 64000)]
    public int Weight { get; set; }
    public WeightedEndpointGroup();
}

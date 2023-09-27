// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

public class WeightedGroupsRoutingOptions
{
    public WeightedGroupSelectionMode SelectionMode { get; set; }
    [Required]
    [Length(1, int.MaxValue)]
    [ValidateEnumeratedItems]
    public IList<WeightedUriEndpointGroup> Groups { get; set; }
    public WeightedGroupsRoutingOptions();
}

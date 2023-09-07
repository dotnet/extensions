// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Data.Validation;

namespace Microsoft.Extensions.Http.Resilience;

public class OrderedGroupsRoutingOptions
{
    [Required]
    [Microsoft.Shared.Data.Validation.Length(1)]
    [ValidateEnumeratedItems]
    public IList<EndpointGroup> Groups { get; set; }
    public OrderedGroupsRoutingOptions();
}

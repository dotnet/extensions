// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Represents the options for collection of endpoint groups that have fixed order.
/// </summary>
/// <remarks>
/// This strategy picks the endpoint groups in he same order as they are specified in the <see cref="P:Microsoft.Extensions.Http.Resilience.OrderedGroupsRoutingOptions.Groups" /> collection.
/// </remarks>
public class OrderedGroupsRoutingOptions
{
    /// <summary>
    /// Gets or sets the collection of ordered endpoints groups.
    /// </summary>
    [Required]
    [Length(1, int.MaxValue)]
    [ValidateEnumeratedItems]
    public IList<UriEndpointGroup> Groups { get; set; }

    public OrderedGroupsRoutingOptions();
}

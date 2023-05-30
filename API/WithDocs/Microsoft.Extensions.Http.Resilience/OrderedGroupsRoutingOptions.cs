// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Data.Validation;

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
    [Microsoft.Shared.Data.Validation.Length(1)]
    [ValidateEnumeratedItems]
    public IList<EndpointGroup> Groups { get; set; }

    public OrderedGroupsRoutingOptions();
}

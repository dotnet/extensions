// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.Resilience;

public class WeightedEndpoint
{
    [Required]
    public Uri? Uri { get; set; }
    [Range(1, 64000)]
    public int Weight { get; set; }
    public WeightedEndpoint();
}

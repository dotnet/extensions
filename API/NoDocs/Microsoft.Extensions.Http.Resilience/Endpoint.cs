// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Http.Resilience;

public class Endpoint
{
    [Required]
    public Uri? Uri { get; set; }
    public Endpoint();
}

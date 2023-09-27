// Assembly 'Microsoft.Extensions.Http.Resilience'

using System;
using System.ComponentModel.DataAnnotations;

namespace Microsoft.Extensions.Http.Resilience;

public class UriEndpoint
{
    [Required]
    public Uri? Uri { get; set; }
    public UriEndpoint();
}

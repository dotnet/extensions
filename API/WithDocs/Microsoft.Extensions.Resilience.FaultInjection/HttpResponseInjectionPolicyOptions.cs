// Assembly 'Microsoft.Extensions.Resilience'

using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Class for http response injection policy options definition.
/// </summary>
public class HttpResponseInjectionPolicyOptions : ChaosPolicyOptionsBase
{
    /// <summary>
    /// Gets or sets the status code to inject.
    /// </summary>
    /// <remarks>
    /// Default is set to <see cref="F:System.Net.HttpStatusCode.BadGateway" />.
    /// </remarks>
    [EnumDataType(typeof(HttpStatusCode))]
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the key to retrieve custom response settings.
    /// </summary>
    /// <remarks>
    /// This field is optional and it defaults to <see langword="null" />.
    /// </remarks>
    public string? HttpContentKey { get; set; }

    public HttpResponseInjectionPolicyOptions();
}

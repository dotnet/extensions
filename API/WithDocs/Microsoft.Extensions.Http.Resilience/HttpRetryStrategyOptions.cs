// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Net.Http;
using Polly.Retry;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// Implementation of the <see cref="T:Polly.Retry.RetryStrategyOptions`1" /> for <see cref="T:System.Net.Http.HttpResponseMessage" /> results.
/// </summary>
public class HttpRetryStrategyOptions : RetryStrategyOptions<HttpResponseMessage>
{
    /// <summary>
    /// Gets or sets a value indicating whether to use the <c>Retry-After</c> header for the retry delays.
    /// </summary>
    /// <value>
    /// Defaults to <see langword="true" />.
    /// </value>
    /// <remarks>
    /// If the property is set to <see langword="true" /> then the generator will resolve the delay
    /// based on the <c>Retry-After</c> header rules, otherwise it will return <see langword="null" /> and the retry strategy
    /// delay will generate the delay based on the configured options.
    /// </remarks>
    public bool ShouldRetryAfterHeader { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.Extensions.Http.Resilience.HttpRetryStrategyOptions" /> class.
    /// </summary>
    /// <remarks>
    /// By default, the options are set to handle only transient failures,
    /// that is, timeouts, 5xx responses, and <see cref="T:System.Net.Http.HttpRequestException" /> exceptions.
    /// </remarks>
    public HttpRetryStrategyOptions();
}

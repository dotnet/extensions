// Assembly 'Microsoft.Extensions.Http.Resilience'

using System.Net.Http;
using Polly;

namespace Microsoft.Extensions.Http.Resilience;

/// <summary>
/// The resilience extensions for <see cref="T:System.Net.Http.HttpRequestMessage" />.
/// </summary>
public static class HttpRequestMessageExtensions
{
    /// <summary>
    /// Gets the <see cref="T:Polly.ResilienceContext" /> from the request message.
    /// </summary>
    /// <param name="requestMessage">The request.</param>
    /// <returns>An instance of <see cref="T:Polly.ResilienceContext" /> or <see langword="null" />.</returns>
    public static ResilienceContext? GetResilienceContext(this HttpRequestMessage requestMessage);

    /// <summary>
    /// Sets the <see cref="T:Polly.ResilienceContext" /> on the request message.
    /// </summary>
    /// <param name="requestMessage">The request.</param>
    /// <param name="resilienceContext">An instance of <see cref="T:Polly.ResilienceContext" />.</param>
    public static void SetResilienceContext(this HttpRequestMessage requestMessage, ResilienceContext? resilienceContext);
}

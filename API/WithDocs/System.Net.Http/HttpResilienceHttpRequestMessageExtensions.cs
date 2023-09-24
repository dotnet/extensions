// Assembly 'Microsoft.Extensions.Http.Resilience'

using Polly;

namespace System.Net.Http;

/// <summary>
/// The resilience extensions for <see cref="T:System.Net.Http.HttpRequestMessage" />.
/// </summary>
public static class HttpResilienceHttpRequestMessageExtensions
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

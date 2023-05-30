// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Diagnostics;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

/// <summary>
/// Interface for implementing enricher for enriching only traces for outgoing HTTP requests.
/// </summary>
public interface IHttpClientTraceEnricher
{
    /// <summary>
    /// Enrich trace with desired tags.
    /// </summary>
    /// <param name="activity"><see cref="T:System.Diagnostics.Activity" /> object to be used to add the required tags to enrich the traces.</param>
    /// <param name="request">HTTP request object associated with the outgoing request for the trace.</param>
    /// <param name="response">HTTP response object associated with the outgoing request for the trace.</param>
    /// <remarks>
    ///  If your enricher fetches some information from <paramref name="request" /> or <paramref name="response" /> to enrich HTTP traces,
    ///  then make sure to check them for <see langword="null" />.
    /// </remarks>
    void Enrich(Activity activity, HttpRequestMessage? request, HttpResponseMessage? response);
}

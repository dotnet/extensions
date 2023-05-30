// Assembly 'Microsoft.AspNetCore.Telemetry'

using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Telemetry;

/// <summary>
/// Interface for implementing enricher for enriching only traces for incoming HTTP requests.
/// </summary>
public interface IHttpTraceEnricher
{
    /// <summary>
    /// Enrich trace with desired tags.
    /// </summary>
    /// <param name="activity"><see cref="T:System.Diagnostics.Activity" /> object to be used to add the required tags to enrich the traces.</param>
    /// <param name="request"><see cref="T:Microsoft.AspNetCore.Http.HttpRequest" /> object associated with the incoming request for the trace.
    /// If your enricher fetches some information from request object to enrich HTTP traces, then make sure to check for <see langword="null" />.</param>
    void Enrich(Activity activity, HttpRequest request);
}

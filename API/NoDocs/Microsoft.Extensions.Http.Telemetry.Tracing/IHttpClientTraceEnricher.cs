// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Diagnostics;
using System.Net.Http;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

public interface IHttpClientTraceEnricher
{
    void Enrich(Activity activity, HttpRequestMessage? request, HttpResponseMessage? response);
}

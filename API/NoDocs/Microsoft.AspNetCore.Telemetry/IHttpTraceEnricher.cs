// Assembly 'Microsoft.AspNetCore.Telemetry'

using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Telemetry;

public interface IHttpTraceEnricher
{
    void Enrich(Activity activity, HttpRequest request);
}

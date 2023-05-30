// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public interface IHttpPathRedactor
{
    string Redact(string routeTemplate, string httpPath, IReadOnlyDictionary<string, DataClassification> parametersToRedact, out int parameterCount);
}

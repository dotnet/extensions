// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Http.Telemetry.Metering;

/// <summary>
/// Statuses for classifying http request result.
/// </summary>
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public enum HttpRequestResultType
{
    /// <summary>
    /// The status code of the http request indicates that the request is successful.
    /// </summary>
    Success = 0,
    /// <summary>
    /// The status code of the http request indicates that this request did not succeed and to be treated as failure.
    /// </summary>
    Failure = 1,
    /// <summary>
    /// The status code of the http request indicates that the request did not succeed but has failed with an error which is expected and acceptable for this request.
    /// </summary>
    /// <remarks>
    /// Expected failures are generally excluded from availability calculations i.e. they are neither
    /// treated as success nor as failures for availability calculation.
    /// </remarks>
    ExpectedFailure = 2
}

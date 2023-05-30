// Assembly 'Microsoft.Extensions.Http.Telemetry'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

/// <summary>
/// Options class for providing configuration parameters to configure outgoing HTTP trace auto collection.
/// </summary>
public class HttpClientTracingOptions
{
    /// <summary>
    /// Gets or sets a value indicating how HTTP request path parameters should be redacted.
    /// </summary>
    /// <value>
    /// The default value is <see cref="F:Microsoft.Extensions.Http.Telemetry.HttpRouteParameterRedactionMode.Strict" />.
    /// </value>
    [Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; }

    /// <summary>
    /// Gets or sets a map between HTTP request parameters and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty <see cref="T:System.Collections.Generic.Dictionary`2" />.
    /// </value>
    /// <remarks>
    /// If a parameter within a controller's action is not annotated with a data classification attribute and
    /// it's not found in this map, it will be redacted as if it was <see cref="P:Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown" />.
    /// If the parameter will not contain sensitive information and shouldn't be redacted, mark it as <see cref="P:Microsoft.Extensions.Compliance.Classification.DataClassification.None" />.
    /// </remarks>
    [Required]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; set; }

    public HttpClientTracingOptions();
}

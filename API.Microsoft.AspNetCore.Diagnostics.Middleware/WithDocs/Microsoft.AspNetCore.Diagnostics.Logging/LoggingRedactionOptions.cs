// Assembly 'Microsoft.AspNetCore.Diagnostics.Middleware'

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Diagnostics;

namespace Microsoft.AspNetCore.Diagnostics.Logging;

/// <summary>
/// Top-level model for redacting incoming HTTP requests and their corresponding responses.
/// </summary>
[Experimental("EXTEXP0013", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public class LoggingRedactionOptions
{
    /// <summary>
    /// Gets or sets a strategy how request path should be logged.
    /// </summary>
    /// <value>
    /// The default value is <see cref="F:Microsoft.AspNetCore.Diagnostics.Logging.IncomingPathLoggingMode.Formatted" />.
    /// </value>
    /// <remarks>
    /// Make sure you add redactors to ensure that sensitive information doesn't find its way into your log records.
    /// This option only applies when the <see cref="P:Microsoft.AspNetCore.Diagnostics.Logging.LoggingRedactionOptions.RequestPathParameterRedactionMode" />
    /// option is not set to <see cref="F:Microsoft.Extensions.Http.Diagnostics.HttpRouteParameterRedactionMode.None" />.
    /// </remarks>
    public IncomingPathLoggingMode RequestPathLoggingMode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating how request path parameter should be redacted.
    /// </summary>
    /// <value>
    /// The default value is <see cref="F:Microsoft.Extensions.Http.Diagnostics.HttpRouteParameterRedactionMode.Strict" />.
    /// </value>
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; }

    /// <summary>
    /// Gets or sets a map between HTTP path parameters and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary.
    /// </value>
    /// <remarks>
    /// If a parameter within a controller's action is not annotated with a data classification attribute and
    /// it's not found in this map, it will be redacted as if it was <see cref="P:Microsoft.Extensions.Compliance.Classification.DataClassification.Unknown" />.
    /// If you don't want a parameter to be redacted, mark it as <see cref="P:Microsoft.Extensions.Compliance.Classification.DataClassification.None" />.
    /// </remarks>
    [Required]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; set; }

    /// <summary>
    /// Gets or sets a map between request headers to be logged and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary, which means that no request header is logged by default.
    /// </value>
    [Required]
    public IDictionary<string, DataClassification> RequestHeadersDataClasses { get; set; }

    /// <summary>
    /// Gets or sets a map between response headers to be logged and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty dictionary, which means that no response header is logged by default.
    /// </value>
    [Required]
    public IDictionary<string, DataClassification> ResponseHeadersDataClasses { get; set; }

    /// <summary>
    /// Gets or sets the set of HTTP paths that should be excluded from logging.
    /// </summary>
    /// <value>
    /// The default value is an empty <see cref="T:System.Collections.Generic.HashSet`1" />.
    /// </value>
    /// <remarks>
    /// Any path added to the set will not be logged.
    /// Paths are case insensitive.
    /// </remarks>
    /// <example>
    /// A typical set of HTTP paths would be:
    /// - "/probe/live".
    /// - "/probe/ready".
    /// </example>
    [Required]
    public ISet<string> ExcludePathStartsWith { get; set; }

    public LoggingRedactionOptions();
}

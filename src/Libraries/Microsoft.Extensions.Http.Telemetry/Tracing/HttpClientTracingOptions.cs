// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Http.Telemetry;

namespace Microsoft.Extensions.Http.Telemetry.Tracing;

/// <summary>
/// Options class for providing configuration parameters to configure outgoing HTTP trace auto collection.
/// </summary>
public class HttpClientTracingOptions
{
    private const HttpRouteParameterRedactionMode DefaultPathParameterRedactionMode = HttpRouteParameterRedactionMode.Strict;

    /// <summary>
    /// Gets or sets a value indicating how HTTP request path parameters should be redacted.
    /// </summary>
    /// <value>
    /// The default value is <see cref="HttpRouteParameterRedactionMode.Strict"/>.
    /// </value>
    [Experimental]
    public HttpRouteParameterRedactionMode RequestPathParameterRedactionMode { get; set; } = DefaultPathParameterRedactionMode;

    /// <summary>
    /// Gets or sets a map between HTTP request parameters and their data classification.
    /// </summary>
    /// <value>
    /// The default value is an empty <see cref="Dictionary{TKey, TValue}"/>.
    /// </value>
    /// <remarks>
    /// If a parameter within a controller's action is not annotated with a data classification attribute and
    /// it's not found in this map, it will be redacted as if it was <see cref="DataClassification.Unknown"/>.
    /// If the parameter will not contain sensitive information and shouldn't be redacted, mark it as <see cref="DataClassification.None"/>.
    /// </remarks>
    [SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "Options pattern.")]
    [Required]
    public IDictionary<string, DataClassification> RouteParameterDataClasses { get; set; }
        = new Dictionary<string, DataClassification>(StringComparer.OrdinalIgnoreCase);
}

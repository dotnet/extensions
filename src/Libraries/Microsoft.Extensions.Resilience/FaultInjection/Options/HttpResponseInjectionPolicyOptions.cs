// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.DataAnnotations;
using System.Net;

namespace Microsoft.Extensions.Resilience.FaultInjection;

/// <summary>
/// Class for http response injection policy options definition.
/// </summary>
public class HttpResponseInjectionPolicyOptions : ChaosPolicyOptionsBase
{
    internal const HttpStatusCode DefaultStatusCode = HttpStatusCode.BadGateway;

    /// <summary>
    /// Gets or sets the status code to inject.
    /// </summary>
    /// <remarks>
    /// Default is set to <see cref="HttpStatusCode.BadGateway"/>.
    /// </remarks>
    [EnumDataType(typeof(HttpStatusCode))]
    public HttpStatusCode StatusCode { get; set; } = DefaultStatusCode;

    /// <summary>
    /// Gets or sets the key to retrieve custom response settings.
    /// </summary>
    /// <remarks>
    /// This field is optional and it defaults to <see langword="null"/>.
    /// </remarks>
    public string? HttpContentKey { get; set; }
}

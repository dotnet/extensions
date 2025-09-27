// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;

namespace Microsoft.Extensions.AI;

/// <summary>Model for expected input to an HTTP handler.</summary>
public sealed class HttpHandlerExpectedInput
{
    /// <summary>Gets or sets the expected request URI.</summary>
    public Uri? Uri { get; set; }

    /// <summary>Gets or sets the expected request body.</summary>
    public string? Body { get; set; }

    /// <summary>
    /// Gets or sets the expected HTTP method.
    /// </summary>
    public HttpMethod? Method { get; set; }
}

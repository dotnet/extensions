// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Http.Logging.Internal;

/// <summary>
/// Methods to read HTTP headers.
/// </summary>
internal interface IHttpHeadersReader
{
    /// <summary>
    /// Read HTTP request headers.
    /// </summary>
    /// <param name="request">An instance of <see cref="HttpRequestMessage"/> to read headers from.</param>
    /// <param name="destination">Destination to save read headers to.</param>
    void ReadRequestHeaders(HttpRequestMessage request, List<KeyValuePair<string, string>>? destination);

    /// <summary>
    /// Read HTTP response headers.
    /// </summary>
    /// <param name="response">An instance of <see cref="HttpResponseMessage"/> to read headers from.</param>
    /// <param name="destination">Destination to save read headers to.</param>
    void ReadResponseHeaders(HttpResponseMessage response, List<KeyValuePair<string, string>>? destination);

    /// <summary>
    /// Redact values by using a <see cref="DataClassification"/>.
    /// </summary>
    /// <param name="value">A value that needs to be redacted.</param>
    /// <param name="classification">An instance of <see cref="DataClassification"/> to redact a value.</param>
    /// <returns>Redacted value.</returns>
    string RedactValue(string value, DataClassification classification);
}

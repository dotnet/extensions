// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;

namespace Microsoft.Extensions.Telemetry.Internal;

/// <summary>
/// HTTP headers redactor.
/// </summary>
internal interface IHttpHeadersRedactor
{
    /// <summary>
    /// Redacts HTTP header values and joins the results into a <see cref="string"/>.
    /// </summary>
    /// <param name="headerValues">HTTP header values.</param>
    /// <param name="classification">Data classification which is used to get an appropriate redactor <see cref="Redactor"/> to redact headers.</param>
    /// <returns>Returns text and parameter segments of route.</returns>
    string Redact(IEnumerable<string> headerValues, DataClassification classification);
}

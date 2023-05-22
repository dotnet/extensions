// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Classification;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;

internal class TestHttpPathRedactor : IHttpPathRedactor
{
    public string Redact(string routeTemplate, string httpPath, IReadOnlyDictionary<string, DataClassification> parametersToRedact, out int parameterCount)
    {
        parameterCount = 0;

        return string.Empty;
    }
}

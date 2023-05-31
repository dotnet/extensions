// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.Http;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Internal;

internal sealed class ConfigureHttpClientInstrumentationOptions : IConfigureOptions<HttpClientInstrumentationOptions>
{
    public void Configure(HttpClientInstrumentationOptions options)
    {
#if NETCOREAPP3_1_OR_GREATER
        options.EnrichWithHttpRequestMessage
#else
        options.EnrichWithHttpWebRequest
#endif
            = ActivityHelper.SetRequest;

#if NETCOREAPP3_1_OR_GREATER
        options.EnrichWithHttpResponseMessage
#else
        options.EnrichWithHttpWebResponse
#endif
            = ActivityHelper.SetResponse;

        options.EnrichWithException = ProcessException;
    }

    private void ProcessException(Activity activity, Exception exception)
    {
        _ = activity.SetTag(Constants.AttributeExceptionType, exception.GetType().FullName);

        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            _ = activity.SetTag(Constants.AttributeExceptionMessage, exception.Message);
        }
    }
}

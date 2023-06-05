// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP3_1_OR_GREATER

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using OpenTelemetry.Instrumentation.AspNetCore;

namespace Microsoft.AspNetCore.Telemetry.Internal;

internal sealed class ConfigureAspNetCoreInstrumentationOptions : IConfigureOptions<AspNetCoreInstrumentationOptions>
{
    public void Configure(AspNetCoreInstrumentationOptions options)
    {
        options.Enrich = static (activity, eventName, rawObject) =>
        {
            if (eventName.Equals(Constants.ActivityStartEvent, StringComparison.Ordinal))
            {
                if (rawObject is HttpRequest request)
                {
                    activity.SetCustomProperty(Constants.CustomPropertyHttpRequest, request);
                }
            }
            else if (eventName.Equals(Constants.ActivityExceptionEvent, StringComparison.Ordinal))
            {
                if (rawObject is Exception exception)
                {
                    _ = activity.SetTag(Constants.AttributeExceptionType, exception.GetType().FullName);

                    if (!string.IsNullOrWhiteSpace(exception.Message))
                    {
                        _ = activity.SetTag(Constants.AttributeExceptionMessage, exception.Message);
                    }
                }
            }
        };
    }
}

#endif

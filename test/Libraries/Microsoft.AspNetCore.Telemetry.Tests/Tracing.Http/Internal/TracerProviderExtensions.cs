// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Microsoft.AspNetCore.Telemetry.Test;

internal static class TracerProviderExtensions
{
    public static TracerProviderBuilder AddTestTraceProcessor(this TracerProviderBuilder builder, BaseProcessor<Activity> processor)
    {
        if (builder is IDeferredTracerProviderBuilder deferredTracerProvider)
        {
            deferredTracerProvider.Configure((_, builder) =>
            {
                builder.AddProcessor(processor);
            });
        }

        return builder;
    }
}

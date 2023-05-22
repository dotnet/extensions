// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Http.Telemetry.Tracing.Test.Internal;

internal static class TestExtensions
{
    public static TracerProviderBuilder AddTestTraceProcessor(this TracerProviderBuilder builder, BaseProcessor<Activity> processor)
    {
        if (builder is IDeferredTracerProviderBuilder deferredTracerProvider)
        {
            deferredTracerProvider.Configure((_, builder) => builder.AddProcessor(processor));
        }

        return builder;
    }
}

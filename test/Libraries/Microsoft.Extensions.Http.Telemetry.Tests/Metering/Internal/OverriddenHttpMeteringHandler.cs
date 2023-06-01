// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Telemetry.Metering;

namespace Microsoft.Extensions.Http.Telemetry.Metering.Test.Internal;

internal sealed class OverriddenHttpMeteringHandler : HttpMeteringHandler
{
    public OverriddenHttpMeteringHandler(
        Meter<HttpMeteringHandler> meter,
        IEnumerable<IOutgoingRequestMetricEnricher> enrichers)
        : base(meter, enrichers)
    {
    }

    public void ExternalDispose(bool dispose)
    {
        Dispose(dispose);
    }
}

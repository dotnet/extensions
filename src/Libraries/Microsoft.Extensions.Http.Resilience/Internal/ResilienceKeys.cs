// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Http.Resilience.Routing.Internal;
using Microsoft.Extensions.Http.Telemetry;
using Polly;

namespace Microsoft.Extensions.Http.Resilience.Internal;

internal static class ResilienceKeys
{
    public static readonly ResiliencePropertyKey<HttpRequestMessage> RequestMessage = new("Resilience.Http.RequestMessage");

    public static readonly ResiliencePropertyKey<RequestRoutingStrategy> RoutingStrategy = new("Resilience.Http.RequestRoutingStrategy");

    public static readonly ResiliencePropertyKey<RequestMessageSnapshot> RequestSnapshot = new("Resilience.Http.Snapshot");

    public static readonly ResiliencePropertyKey<RequestMetadata> RequestMetadata = new(TelemetryConstants.RequestMetadataKey);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Diagnostics;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Internal;

internal static class TelemetryCommonHttpExtensions
{
    public static IServiceCollection AddHttpHeadersRedactor(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddActivatedSingleton<IHttpHeadersRedactor, HttpHeadersRedactor>();
        return services;
    }

    public static IServiceCollection AddOutgoingRequestContext(this IServiceCollection services)
    {
        services.TryAddSingleton<IOutgoingRequestContext, OutgoingRequestContext>();
        return services;
    }
}

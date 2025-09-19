// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Http.Diagnostics;

/// <summary>
/// Extensions for common telemetry utilities.
/// </summary>
internal static class TelemetryCommonExtensions
{
    internal const string UnsupportedEnumValueExceptionMessage = $"Unsupported value for enum type {nameof(HttpRouteParameterRedactionMode)}.";

    /// <summary>
    /// Adds http route processing elements.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> object.</param>
    /// <returns>Returns <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddHttpRouteProcessor(this IServiceCollection services)
    {
        services.TryAddActivatedSingleton<HttpRouteParser, DefaultHttpRouteParser>();
        services.TryAddActivatedSingleton<HttpRouteFormatter, DefaultHttpRouteFormatter>();
        return services;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.Extensions.Telemetry.Internal;

/// <summary>
/// Extensions for common telemetry utilities.
/// </summary>
internal static class TelemetryCommonExtensions
{
    internal const string UnsupportedEnumValueExceptionMessage = $"Unsupported value for enum type {nameof(HttpRouteParameterRedactionMode)}.";

    /// <summary>
    /// Gets the request name.
    /// </summary>
    /// <param name="metadata">Request metadata.</param>
    /// <returns>Returns the name of the request.</returns>
    public static string GetRequestName(this RequestMetadata metadata)
    {
        _ = Throw.IfNull(metadata);

        if (metadata.RequestName == TelemetryConstants.Unknown)
        {
            return metadata.RequestRoute;
        }

        return metadata.RequestName;
    }

    /// <summary>
    /// Adds http route processing elements.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> object.</param>
    /// <returns>Returns <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddHttpRouteProcessor(this IServiceCollection services)
    {
        services.TryAddActivatedSingleton<IHttpRouteParser, HttpRouteParser>();
        services.TryAddActivatedSingleton<IHttpRouteFormatter, HttpRouteFormatter>();
        return services;
    }

    /// <summary>
    /// Adds HTTP headers redactor.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> object.</param>
    /// <returns>Returns <see cref="IServiceCollection"/> object.</returns>
    public static IServiceCollection AddHttpHeadersRedactor(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        services.TryAddActivatedSingleton<IHttpHeadersRedactor, HttpHeadersRedactor>();
        return services;
    }

    /// <summary>
    /// Adds <see cref="IOutgoingRequestContext"/> instance to services collection.
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/> object instance.</param>
    /// <returns><see cref="IServiceCollection"/> object for chaining.</returns>
    public static IServiceCollection AddOutgoingRequestContext(this IServiceCollection services)
    {
        services.TryAddSingleton<IOutgoingRequestContext, OutgoingRequestContext>();
        return services;
    }
}

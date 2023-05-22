// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.ExceptionSummarization;
using Microsoft.Extensions.Http.Telemetry;
using Microsoft.Extensions.Resilience;
using Microsoft.Shared.Diagnostics;
using Microsoft.Shared.Text;

namespace Microsoft.Extensions.Http.Resilience.Internal;

/// <summary>
/// Extension class for the Service Collection DI container.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class HttpPolicyFactoryServiceCollectionExtensions
{
    private static readonly ServiceDescriptor _serviceDescriptor = ServiceDescriptor.Singleton<Marker, Marker>();

    /// <summary>
    /// Configures the failure result dimensions that will be emitted for Http failures, by exploring the inner exceptions and their properties.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <returns>The input <paramref name="services"/>.</returns>
    public static IServiceCollection ConfigureHttpFailureResultContext(this IServiceCollection services)
    {
        _ = Throw.IfNull(services);

        // don't add any new service if this method is called multiple times
        if (services.Contains(_serviceDescriptor))
        {
            return services;
        }

        services.Add(_serviceDescriptor);

        return services
            .AddExceptionSummarizer(b => b.AddHttpProvider())
            .ConfigureFailureResultContext<HttpResponseMessage>((response) =>
            {
                if (response != null)
                {
                    var statusCodeName = response.StatusCode.ToInvariantString();
                    if (string.IsNullOrEmpty(statusCodeName) || char.IsDigit(statusCodeName[0]))
                    {
                        statusCodeName = TelemetryConstants.Unknown;
                    }

                    return FailureResultContext.Create(failureReason: ((int)response.StatusCode).ToInvariantString(), additionalInformation: statusCodeName);
                }

                return FailureResultContext.Create();
            });
    }

    private sealed class Marker
    {
    }
}

// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.Common'

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public static class CommonHealthChecksExtensions
{
    public static IHealthChecksBuilder AddApplicationLifecycleHealthCheck(this IHealthChecksBuilder builder, params string[] tags);
    public static IHealthChecksBuilder AddApplicationLifecycleHealthCheck(this IHealthChecksBuilder builder, IEnumerable<string> tags);
    public static IHealthChecksBuilder AddManualHealthCheck(this IHealthChecksBuilder builder, params string[] tags);
    public static IHealthChecksBuilder AddManualHealthCheck(this IHealthChecksBuilder builder, IEnumerable<string> tags);
    public static void ReportHealthy(this IManualHealthCheck manualHealthCheck);
    public static void ReportUnhealthy(this IManualHealthCheck manualHealthCheck, string reason);
    public static IServiceCollection AddTelemetryHealthCheckPublisher(this IServiceCollection services);
    [Experimental("EXTEXP0007", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddTelemetryHealthCheckPublisher(this IServiceCollection services, IConfigurationSection section);
    [Experimental("EXTEXP0007", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IServiceCollection AddTelemetryHealthCheckPublisher(this IServiceCollection services, Action<TelemetryHealthCheckPublisherOptions> configure);
}

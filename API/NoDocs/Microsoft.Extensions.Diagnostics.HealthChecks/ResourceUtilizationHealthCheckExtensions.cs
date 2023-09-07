// Assembly 'Microsoft.Extensions.Diagnostics.HealthChecks.ResourceUtilization'

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Diagnostics.HealthChecks;

public static class ResourceUtilizationHealthCheckExtensions
{
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, params string[] tags);
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, IEnumerable<string> tags);
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, IConfigurationSection section);
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, IConfigurationSection section, params string[] tags);
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, IConfigurationSection section, IEnumerable<string> tags);
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, Action<ResourceUtilizationHealthCheckOptions> configure);
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, Action<ResourceUtilizationHealthCheckOptions> configure, params string[] tags);
    public static IHealthChecksBuilder AddResourceUtilizationHealthCheck(this IHealthChecksBuilder builder, Action<ResourceUtilizationHealthCheckOptions> configure, IEnumerable<string> tags);
}

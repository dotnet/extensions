// Assembly 'Microsoft.Extensions.Telemetry'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Telemetry.Metering;

public static class EventCountersExtensions
{
    public static IServiceCollection AddEventCounterCollector(this IServiceCollection services, Action<EventCountersCollectorOptions> configure);
    public static IServiceCollection AddEventCounterCollector(this IServiceCollection services, IConfigurationSection section);
}

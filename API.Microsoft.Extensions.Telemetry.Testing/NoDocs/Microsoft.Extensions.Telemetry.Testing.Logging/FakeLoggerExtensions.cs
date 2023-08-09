// Assembly 'Microsoft.Extensions.Telemetry.Testing'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Telemetry.Testing.Logging;

public static class FakeLoggerExtensions
{
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder, IConfigurationSection section);
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder, Action<FakeLogCollectorOptions> configure);
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder);
    public static IServiceCollection AddFakeLogging(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddFakeLogging(this IServiceCollection services, Action<FakeLogCollectorOptions> configure);
    public static IServiceCollection AddFakeLogging(this IServiceCollection services);
    public static FakeLogCollector GetFakeLogCollector(this IServiceProvider services);
}

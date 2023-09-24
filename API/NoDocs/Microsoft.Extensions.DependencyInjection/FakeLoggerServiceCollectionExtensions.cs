// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.Extensions.DependencyInjection;

public static class FakeLoggerServiceCollectionExtensions
{
    public static IServiceCollection AddFakeLogging(this IServiceCollection services, IConfigurationSection section);
    public static IServiceCollection AddFakeLogging(this IServiceCollection services, Action<FakeLogCollectorOptions> configure);
    public static IServiceCollection AddFakeLogging(this IServiceCollection services);
}

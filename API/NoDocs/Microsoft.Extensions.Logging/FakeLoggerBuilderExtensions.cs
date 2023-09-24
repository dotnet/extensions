// Assembly 'Microsoft.Extensions.Diagnostics.Testing'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.Extensions.Logging;

public static class FakeLoggerBuilderExtensions
{
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder, IConfigurationSection section);
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder, Action<FakeLogCollectorOptions> configure);
    public static ILoggingBuilder AddFakeLogging(this ILoggingBuilder builder);
}

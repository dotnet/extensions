// Assembly 'Microsoft.Extensions.Http.Diagnostics'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class HttpClientLoggingHttpClientBuilderExtensions
{
    public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder);
    public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder, IConfigurationSection section);
    public static IHttpClientBuilder AddExtendedHttpClientLogging(this IHttpClientBuilder builder, Action<LoggingOptions> configure);
}

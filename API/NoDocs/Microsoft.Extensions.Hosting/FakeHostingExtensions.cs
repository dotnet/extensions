// Assembly 'Microsoft.Extensions.Hosting.Testing'

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.Extensions.Hosting;

[Experimental("EXTEXP0009", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class FakeHostingExtensions
{
    public static Task StartAndStopAsync(this IHostedService service, CancellationToken cancellationToken = default(CancellationToken));
    public static FakeLogCollector GetFakeLogCollector(this IHost host);
    public static FakeRedactionCollector GetFakeRedactionCollector(this IHost host);
    public static IHostBuilder AddFakeLoggingOutputSink(this IHostBuilder builder, Action<string> callback);
    public static IHostBuilder Configure(this IHostBuilder builder, Action<IHostBuilder> configure);
    public static IHostBuilder ConfigureHostConfiguration(this IHostBuilder builder, params (string key, string value)[] configurations);
    public static IHostBuilder ConfigureHostConfiguration(this IHostBuilder builder, string key, string value);
    public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder builder, params (string key, string value)[] configurations);
    public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder builder, string key, string value);
}

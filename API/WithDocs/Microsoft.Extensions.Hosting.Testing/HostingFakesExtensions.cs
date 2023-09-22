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

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Extension methods supporting host unit testing scenarios.
/// </summary>
[Experimental("EXTEXP0009", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public static class HostingFakesExtensions
{
    /// <summary>
    /// Starts and immediately stops the service.
    /// </summary>
    /// <param name="service">The tested service.</param>
    /// <param name="cancellationToken">Cancellation token. See <see cref="T:System.Threading.CancellationToken" />.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> representing the asynchronous operation.</returns>
    public static Task StartAndStopAsync(this IHostedService service, CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// Gets the object that collects log records sent to the fake logger.
    /// </summary>
    /// <param name="host">An <see cref="T:Microsoft.Extensions.Hosting.IHost" /> instance.</param>
    /// <exception cref="T:System.InvalidOperationException">No collector exists in the provider.</exception>
    /// <returns>The collector that tracks records logged to fake loggers.</returns>
    public static FakeLogCollector GetFakeLogCollector(this IHost host);

    /// <summary>
    /// Gets the object reporting all redactions performed.
    /// </summary>
    /// <param name="host">An <see cref="T:Microsoft.Extensions.Hosting.IHost" /> instance.</param>
    /// <exception cref="T:System.InvalidOperationException">No collector exists in the provider.</exception>
    /// <returns>The collector that tracks redactions performed on log messages.</returns>
    public static FakeRedactionCollector GetFakeRedactionCollector(this IHost host);

    /// <summary>
    /// Adds an action invoked on each log message.
    /// </summary>
    /// <param name="builder">An <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</param>
    /// <param name="callback">The action to invoke on each log message.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</returns>
    public static IHostBuilder AddFakeLoggingOutputSink(this IHostBuilder builder, Action<string> callback);

    /// <summary>
    /// Exposes <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> for changes via a delegate.
    /// </summary>
    /// <param name="builder">An <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</param>
    /// <param name="configure">Configures the <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</returns>
    /// <remarks>Designed to ease host configuration in unit tests by defining common configuration methods.</remarks>
    public static IHostBuilder Configure(this IHostBuilder builder, Action<IHostBuilder> configure);

    public static IHostBuilder ConfigureHostConfiguration(this IHostBuilder builder, params (string key, string value)[] configurations);

    /// <summary>
    /// Adds a configuration value.
    /// </summary>
    /// <param name="builder">An <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</returns>
    public static IHostBuilder ConfigureHostConfiguration(this IHostBuilder builder, string key, string value);

    public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder builder, params (string key, string value)[] configurations);

    /// <summary>
    /// Adds a configuration value.
    /// </summary>
    /// <param name="builder">An <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</param>
    /// <param name="key">The configuration key.</param>
    /// <param name="value">The configuration value.</param>
    /// <returns>The <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> instance.</returns>
    public static IHostBuilder ConfigureAppConfiguration(this IHostBuilder builder, string key, string value);
}

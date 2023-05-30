// Assembly 'Microsoft.Extensions.Hosting.Testing'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting.Testing;

/// <summary>
/// Unit testing friendly configured host.
/// </summary>
public sealed class FakeHost : IHost, IDisposable
{
    /// <summary>
    /// Gets the programs configured services.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Creates an instance of <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> to configure and build the host.
    /// </summary>
    /// <returns>An instance of <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" />.</returns>
    public static IHostBuilder CreateBuilder();

    /// <summary>
    /// Creates an instance of <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> to configure and build the host.
    /// </summary>
    /// <param name="configure">Use to configure the <see cref="T:Microsoft.Extensions.Hosting.Testing.FakeHostOptions" /> instance.</param>
    /// <returns>An instance of <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" />.</returns>
    public static IHostBuilder CreateBuilder(Action<FakeHostOptions> configure);

    /// <summary>
    /// Creates an instance of <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" /> to configure and build the host.
    /// </summary>
    /// <param name="options">An <see cref="T:Microsoft.Extensions.Hosting.Testing.FakeHostOptions" /> instance.</param>
    /// <returns>An instance of <see cref="T:Microsoft.Extensions.Hosting.IHostBuilder" />.</returns>
    public static IHostBuilder CreateBuilder(FakeHostOptions options);

    /// <summary>
    /// Start the program.
    /// </summary>
    /// <param name="cancellationToken">Used to abort program start.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that will be completed when the <see cref="T:Microsoft.Extensions.Hosting.IHost" /> starts.</returns>
    /// <remarks>If no cancellation token is given, a new one using <see cref="P:Microsoft.Extensions.Hosting.Testing.FakeHostOptions.StartUpTimeout" /> is used.</remarks>
    public Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// Attempts to gracefully stop the program.
    /// </summary>
    /// <param name="cancellationToken">Used to indicate when stop should no longer be graceful.</param>
    /// <returns>A <see cref="T:System.Threading.Tasks.Task" /> that will be completed when the <see cref="T:Microsoft.Extensions.Hosting.IHost" /> stops.</returns>
    /// <remarks>If no cancellation token is given, a new one using <see cref="P:Microsoft.Extensions.Hosting.Testing.FakeHostOptions.StartUpTimeout" /> is used.</remarks>
    public Task StopAsync(CancellationToken cancellationToken = default(CancellationToken));

    /// <summary>
    /// Disposes the <see cref="T:Microsoft.Extensions.Hosting.IHost" /> instance.
    /// </summary>
    /// <remarks>Tries to gracefully shut down the host. Can be called multiple times.</remarks>
    public void Dispose();
}

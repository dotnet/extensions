// Assembly 'Microsoft.Extensions.Hosting.Testing'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Hosting.Testing;

public sealed class FakeHost : IHost, IDisposable
{
    public IServiceProvider Services { get; }
    public static IHostBuilder CreateBuilder();
    public static IHostBuilder CreateBuilder(Action<FakeHostOptions> configure);
    public static IHostBuilder CreateBuilder(FakeHostOptions options);
    public Task StartAsync(CancellationToken cancellationToken = default(CancellationToken));
    public Task StopAsync(CancellationToken cancellationToken = default(CancellationToken));
    public void Dispose();
}

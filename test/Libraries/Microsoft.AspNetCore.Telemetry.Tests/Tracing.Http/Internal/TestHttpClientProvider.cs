// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
#if NETCOREAPP3_1_OR_GREATER
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
#else
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Routing;
#endif
using Microsoft.Extensions.Compliance.Redaction;
using OpenTelemetry.Trace;

namespace Microsoft.AspNetCore.Telemetry.Test.Internal;

internal class TestHttpClientProvider : IDisposable
{
    private readonly Action<TracerProviderBuilder> _configureBuilder;
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly Action<IRedactionBuilder>? _configureRedaction;
    private readonly string _endpointPattern;
    private bool _disposedValue;
    private System.Net.Http.HttpClient? _httpClient;
#if NETCOREAPP3_1_OR_GREATER
    private IHost? _host;
#else
    private TestServer? _server;
#endif
    public TestHttpClientProvider(
        string endpointPattern,
        Action<TracerProviderBuilder> configureBuilder,
        Action<IRedactionBuilder>? configureRedaction = null,
        Action<IServiceCollection>? configureServices = null)
    {
        _endpointPattern = endpointPattern;
        _configureBuilder = configureBuilder;
        _configureRedaction = configureRedaction;
        _configureServices = configureServices;
    }

    public IServiceProvider? Services { get; private set; }

    public async Task<System.Net.Http.HttpClient> GetHttpClientAsync()
    {
#if NETCOREAPP3_1_OR_GREATER
        _host = await FakeHost.CreateBuilder(options => options.FakeRedaction = false)
            .ConfigureWebHost(webBuilder => webBuilder
                .UseTestServer()
                .TryConfigureServices(_configureServices)
                .ConfigureServices(services => services
                    .AddRouting()
                    .AddOpenTelemetry().WithTracing(_configureBuilder).Services
                    .TryConfigureRedaction(_configureRedaction))
                .Configure(app => app
                    .UseRouting()
                    .UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet(_endpointPattern, async context =>
                        {
                            context.Response.StatusCode = 200;
                            await context.Response.WriteAsync("TestCompleted");
                        });
                    })))
            .StartAsync();
        Services = _host.Services;
        _httpClient = _host.GetTestClient();
#else
        var webHostBuilder = new WebHostBuilder()
            .TryConfigureServices(_configureServices)
            .ConfigureServices(services => services
                .AddMvc()
                .SetCompatibilityVersion(AspNetCore.Mvc.CompatibilityVersion.Version_2_2)
                .Services
                .AddRouting()
                .AddOpenTelemetry().WithTracing(_configureBuilder).Services
                .TryConfigureRedaction(_configureRedaction))
            .Configure(app => app
                .UseEndpointRouting()
                .UseRouter(routes =>
                {
                    routes.MapMiddlewareGet(_endpointPattern, builder => builder.Run(async context =>
                    {
                        context.Response.StatusCode = 200;
                        await context.Response.WriteAsync("TestCompleted");
                    }));
                })
                .UseMvc());

        _server = new TestServer(webHostBuilder);
        await _server.Host.StartAsync();

        Services = _server.Host.Services;
        _httpClient = _server.CreateClient();
#endif

        return _httpClient;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _httpClient?.Dispose();
#if NETCOREAPP3_1_OR_GREATER
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                _host?.StopAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                _host?.Dispose();
#else
                _server?.Dispose();
#endif
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.Http.Resilience.Test;

public sealed partial class HttpClientBuilderExtensionsTests
{
    private static readonly Uri _defaultFallbackUri = new("Http://dummy.uri");

    [Fact]
    public async Task BuildHostBuilder_WithConfigureFallbackOptions_ShouldNotThrow()
    {
        using var host = FakeHost.CreateBuilder()
            .ConfigureServices(services => services
                .AddHttpClient("client", client => client.BaseAddress = new Uri("http://localhost:8080/"))
                .AddFallbackHandler(opts => opts.BaseFallbackUri = new Uri("http://localhost:9090")))
            .Build();

        // Start host.
        var startTask = host.RunAsync();

        // When start logic is complete, stop it.
        await host.StopAsync();

        // Await, so the task becomes completed and assert.
        await startTask;
        Assert.True(startTask.IsCompleted);
    }

    [InlineData(Args.Configure)]
    [InlineData(Args.Section)]
    [InlineData(Args.ConfigureAndSection)]
    [Theory]
    public void AddFallbackHandler_InvalidConfiguration_OptionsValidationException(Args args)
    {
        AddFallbackHandler(args, new ConfigurationBuilder().Build().GetSection(""), options => { });

        Assert.Throws<OptionsValidationException>(() => CreateClient());
    }

    [InlineData(Args.Configure)]
    [InlineData(Args.Section)]
    [InlineData(Args.ConfigureAndSection)]
    [Theory]
    public void AddFallbackHandler_ValidConfiguration_OptionsValidationException(Args args)
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "section:BaseFallbackUri", _defaultFallbackUri.ToString() }
        });
        var section = builder.Build().GetSection("section");

        var result = AddFallbackHandler(args, section, options => options.BaseFallbackUri = _defaultFallbackUri);

        Assert.NotNull(CreateClient());
    }

    [Fact]
    public async Task AddFallbackHandler_EnsureWorksCorrectly()
    {
        var urls = new List<Uri>();
        using var testHandler = new TestHandler
        {
            ResponseFactory = r =>
            {
                urls.Add(r.RequestUri!);

                if (urls.Count == 1)
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);
                }
                else
                {
                    return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                }
            }
        };

        _builder.AddFallbackHandler(options => options.BaseFallbackUri = _defaultFallbackUri);
        _builder.AddHttpMessageHandler(() => testHandler);

        var client = CreateClient();

        var response = await client.GetAsync("https://dummy-host/path");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, urls.Count);
        Assert.Equal("https://dummy-host/path", urls[0].ToString());
        Assert.Equal($"{_defaultFallbackUri}path", urls[1].ToString());
    }

    private System.Net.Http.HttpClient CreateClient() => _builder.Services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient(BuilderName);

    private IHttpClientBuilder AddFallbackHandler(Args args, IConfigurationSection? section, Action<FallbackClientHandlerOptions>? configure)
    {
        return args switch
        {
            Args.Section => _builder.AddFallbackHandler(section!),
            Args.Configure => _builder.AddFallbackHandler(configure!),
            Args.ConfigureAndSection => _builder.AddFallbackHandler(section!, configure!),
            _ => throw new NotSupportedException(),
        };
    }

    public enum Args
    {
        Section,
        Configure,
        ConfigureAndSection
    }

    private class TestHandler : DelegatingHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage>? ResponseFactory { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(ResponseFactory!.Invoke(request));
        }
    }
}

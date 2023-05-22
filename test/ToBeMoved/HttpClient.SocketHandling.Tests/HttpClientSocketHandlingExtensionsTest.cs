// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using AutoFixture;
using AutoFixture.Kernel;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Testing;
using Microsoft.Extensions.HttpClient.SocketHandling.Test.Utils;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.Extensions.HttpClient.SocketHandling.Test;

public class HttpClientSocketHandlingExtensionsTest
{
    [Fact]
    public void AddSocketsHandler_NotUsingBuilder_ReturnsOriginalBuilderInstance()
    {
        var services = new ServiceCollection();

        var originalBuilder = services.AddHttpClient<HttpClientSocketHandlingExtensionsTest>();
        var returnedBuilder = originalBuilder.AddSocketsHttpHandler();

        returnedBuilder.Should().Be(originalBuilder);
    }

    [Fact]
    public void AddSocketsHandler_UsingBuilder_ReturnsOriginalBuilderInstance()
    {
        var services = new ServiceCollection();

        var originalBuilder = services.AddHttpClient<HttpClientSocketHandlingExtensionsTest>();
        var returnedBuilder = originalBuilder.AddSocketsHttpHandler(builder => builder.DisableRemoteCertificateValidation());

        returnedBuilder.Should().Be(originalBuilder);
    }

    [Fact]
    public void AddSocketsHandler_WithDefaultOptions_ShouldUpdateHandler()
    {
        var services = new ServiceCollection();

        _ = services
            .AddHttpClient<HttpClientSocketHandlingExtensionsTest>()
            .AddSocketsHttpHandler();

        using var provider = services.BuildServiceProvider();
        var primaryHandler = provider.ResolveHttpPrimaryHandler<HttpClientSocketHandlingExtensionsTest>();

        primaryHandler.ExtractOptions().Should().BeEquivalentTo(new SocketsHttpHandlerOptions());
    }

    [Fact]
    public void AddSocketsHandler_WithExplicitOptions_ShouldUpdateHandler()
    {
        SocketsHttpHandlerOptions? socketOptions = null;
        var services = new ServiceCollection();

        _ = services
            .AddHttpClient<HttpClientSocketHandlingExtensionsTest>()
            .AddSocketsHttpHandler();

        services.Configure<SocketsHttpHandlerOptions>(nameof(HttpClientSocketHandlingExtensionsTest), o =>
        {
            new AutoPropertiesCommand().Execute(o, new SpecimenContext(CreateFixture()));
            socketOptions = o; // capture
        });

        using var provider = services.BuildServiceProvider();
        var primaryHandler = provider.ResolveHttpPrimaryHandler<HttpClientSocketHandlingExtensionsTest>();

        primaryHandler.ExtractOptions().Should().BeEquivalentTo(socketOptions);
    }

    [Fact]
    public void AddSocketsHandler_WithInPlaceOptions_ShouldUpdateHandler()
    {
        SocketsHttpHandlerOptions? socketOptions = null;
        var services = new ServiceCollection();

        _ = services
            .AddHttpClient<HttpClientSocketHandlingExtensionsTest>()
            .AddSocketsHttpHandler(builder =>
            {
                builder.ConfigureOptions(options =>
                {
                    new AutoPropertiesCommand().Execute(options, new SpecimenContext(CreateFixture()));
                    socketOptions = options; // capture
                });
            });

        using var provider = services.BuildServiceProvider();
        var primaryHandler = provider.ResolveHttpPrimaryHandler<HttpClientSocketHandlingExtensionsTest>();

        primaryHandler.ExtractOptions().Should().BeEquivalentTo(socketOptions);
    }

    [Fact]
    public void AddSocketsHandler_WithConfigSection_ShouldUpdateHandler()
    {
        const string ConnectTimeout = "00:04:56";
        const string ConfigSection = "sockets";

        using var host = FakeHost.CreateBuilder()
            .ConfigureHostConfiguration($"{ConfigSection}:ConnectTimeout", ConnectTimeout)
            .ConfigureServices((context, services) => services
                .AddHttpClient<HttpClientSocketHandlingExtensionsTest>()
                .AddSocketsHttpHandler(builder => builder.ConfigureOptions(context.Configuration.GetSection(ConfigSection))))
            .Build();

        var primaryHandler = host.Services.ResolveHttpPrimaryHandler<HttpClientSocketHandlingExtensionsTest>();
        var socketsOptions = host.Services.GetRequiredService<IOptionsMonitor<SocketsHttpHandlerOptions>>()
            .Get(nameof(HttpClientSocketHandlingExtensionsTest));

        socketsOptions.ConnectTimeout.Should().Be(TimeSpan.Parse(ConnectTimeout, CultureInfo.InvariantCulture));
        primaryHandler.ExtractOptions().Should().BeEquivalentTo(socketsOptions);
    }

    [Fact]
    public void AddSocketsHandler_WithDefaultOptions_ChangesAutomaticDecompression()
    {
        var services = new ServiceCollection();

        _ = services
            .AddHttpClient<HttpClientSocketHandlingExtensionsTest>()
            .AddSocketsHttpHandler();

        using var provider = services.BuildServiceProvider();
        var primaryHandler = provider.ResolveHttpPrimaryHandler<HttpClientSocketHandlingExtensionsTest>();

        ((SocketsHttpHandler)primaryHandler).AutomaticDecompression.Should().Be(DecompressionMethods.All);
    }

    [Fact]
    public void DisableRemoteCertificateValidation_WithBogusArguments_ReturnsTrue()
    {
        var services = new ServiceCollection();

        _ = services
            .AddHttpClient<HttpClientSocketHandlingExtensionsTest>()
            .AddSocketsHttpHandler(builder => builder.DisableRemoteCertificateValidation());

        using var provider = services.BuildServiceProvider();
        var primaryHandler = provider.ResolveHttpPrimaryHandler<HttpClientSocketHandlingExtensionsTest>();
        var options = ((SocketsHttpHandler)primaryHandler).SslOptions;

        options.RemoteCertificateValidationCallback!(null!, null!, null!, SslPolicyErrors.RemoteCertificateNameMismatch)
            .Should().BeTrue();
    }

    [Fact]
    public void ConfigureClientCertificate_WithCertificate_AppliesIt()
    {
        using var certificate = new X509Certificate2(Array.Empty<byte>());

        var services = new ServiceCollection();

        _ = services
            .AddHttpClient<HttpClientSocketHandlingExtensionsTest>()
            .AddSocketsHttpHandler(builder => builder.ConfigureClientCertificate(_ => certificate));

        using var provider = services.BuildServiceProvider();
        var primaryHandler = provider.ResolveHttpPrimaryHandler<HttpClientSocketHandlingExtensionsTest>();
        var callback = ((SocketsHttpHandler)primaryHandler).SslOptions.LocalCertificateSelectionCallback;

        callback!(null!, null!, new X509CertificateCollection(), null!, null!).Should().Be(certificate);
    }

    [Fact]
    public void ConfigureClientCertificate_NullCertificate_ThrowsOnBuild()
    {
        var services = new ServiceCollection();

        _ = services
            .AddHttpClient<HttpClientSocketHandlingExtensionsTest>()
            .AddSocketsHttpHandler(builder => builder.ConfigureClientCertificate(_ => null!));

        using var provider = services.BuildServiceProvider();
        var executingBuilder = () => provider.ResolveHttpPrimaryHandler<HttpClientSocketHandlingExtensionsTest>();

        executingBuilder.Should().Throw<InvalidDataException>()
            .WithMessage("The parameter clientCertificate returned null when called.");
    }

    [Fact]
    public void AddSocketsHandler_WithConfigureAction_ShouldUpdateHandler()
    {
        var sslClientAuthenticationOptions = new SslClientAuthenticationOptions();
        var services = new ServiceCollection();

        _ = services
            .AddHttpClient<HttpClientSocketHandlingExtensionsTest>()
            .AddSocketsHttpHandler(builder =>
            {
                builder.ConfigureHandler(handler => handler.SslOptions = sslClientAuthenticationOptions);
            });

        using var provider = services.BuildServiceProvider();
        var primaryHandler = provider.ResolveHttpPrimaryHandler<HttpClientSocketHandlingExtensionsTest>();

        ((SocketsHttpHandler)primaryHandler).SslOptions.Should().Be(sslClientAuthenticationOptions);
    }

    [Fact]
    public void AddSocketsHandler_WithConfigureActionWithServiceProvider_ShouldUseServiceProvider()
    {
        IServiceProvider? capturedServices = null;
        var services = new ServiceCollection();

        _ = services
            .AddHttpClient<HttpClientSocketHandlingExtensionsTest>()
            .AddSocketsHttpHandler(builder =>
            {
                builder.ConfigureHandler((provider, _) =>
                {
                    capturedServices = provider;
                });
            });

        using var provider = services.BuildServiceProvider();
        _ = provider.ResolveHttpPrimaryHandler<HttpClientSocketHandlingExtensionsTest>();

        capturedServices.Should().Be(provider.GetRequiredService<IServiceProvider>());
    }

    [Fact]
    public void AddSocketsHandler_WithConfigureActionWithServiceProvider_ShouldUpdateHandler()
    {
        var sslClientAuthenticationOptions = new SslClientAuthenticationOptions();
        var services = new ServiceCollection();

        _ = services
            .AddHttpClient<HttpClientSocketHandlingExtensionsTest>()
            .AddSocketsHttpHandler(builder =>
            {
                builder.ConfigureHandler((_, handler) =>
                {
                    handler.SslOptions = sslClientAuthenticationOptions;
                });
            });

        using var provider = services.BuildServiceProvider();
        var primaryHandler = provider.ResolveHttpPrimaryHandler<HttpClientSocketHandlingExtensionsTest>();

        ((SocketsHttpHandler)primaryHandler).SslOptions.Should().Be(sslClientAuthenticationOptions);
    }

    private static Fixture CreateFixture()
    {
        var customizedFixture = new Fixture();

        customizedFixture.Customize<TimeSpan>(c =>
        {
            // SocketsHttpHandler setters enforce some values to be bigger than 1 seconds therefore this customization.
            return c.FromFactory(() => TimeSpan.FromSeconds(1) + TimeSpan.FromTicks(customizedFixture.Create<int>()));
        });

        return customizedFixture;
    }
}

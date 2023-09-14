// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.HeaderParsing.Test;

public class HeaderParsingExtensionsTests
{
    [Fact]
    public void AddHeaderParsing_configures_options_using_delegate()
    {
        var services = new ServiceCollection()
            .AddHeaderParsing(options => options.DefaultValues.Add("Test", "9"))
            .BuildServiceProvider();

        var options = services.GetRequiredService<IOptions<HeaderParsingOptions>>().Value;

        Assert.True(options.DefaultValues.ContainsKey("Test"));
    }

    [Fact]
    public void AddHeaderParsing_configures_invalid_options()
    {
        var services = new ServiceCollection()
            .AddHeaderParsing(options => options.DefaultMaxCachedValuesPerHeader = -1)
            .BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<HeaderParsingOptions>>().Value);
    }

    [Fact]
    public void AddHeaderParsing_configures_invalid_options_MaxCachedValuesPerHeader()
    {
        var services = new ServiceCollection()
            .AddHeaderParsing(options => options.MaxCachedValuesPerHeader["Date"] = -1)
            .BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<HeaderParsingOptions>>().Value);
    }

    [Fact]
    public void AddHeaderParsing_section()
    {
        var services = new ServiceCollection()
            .AddHeaderParsing(GetConfigurationSection())
            .BuildServiceProvider();

        var options = services.GetRequiredService<IOptions<HeaderParsingOptions>>().Value;

        Assert.Equal(123, options.DefaultMaxCachedValuesPerHeader);

        static IConfigurationSection GetConfigurationSection()
        {
            HeaderParsingOptions options;

            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {
                        $"{nameof(HeaderParsingOptions)}:{nameof(options.DefaultMaxCachedValuesPerHeader)}",
                        "123"
                    },
                })
                .Build()
                .GetSection($"{nameof(HeaderParsingOptions)}");
        }
    }

    [Fact]
    public void Register_header_registers_given_header()
    {
        var headerKey = new HeaderKey<DateTimeOffset>("Test", CommonHeaders.Date.ParserInstance!, 0);

        var headerSetup = CommonHeaders.Date;
        var headerRegistry = new Mock<IHeaderRegistry>();
        headerRegistry.Setup(x => x.Register(headerSetup)).Returns(headerKey);

        var context = CreateContext(new ServiceCollection().AddSingleton(headerRegistry.Object));
        Assert.Equal(headerKey, RegisterHeader(context, headerSetup));
    }

    [Fact]
    public void GetHeaderParsing_caches_and_returns_header_parsing_feature()
    {
        var context = CreateContext(new ServiceCollection().AddHeaderParsing());

        var feature = context.Request.GetHeaderParsing();

        Assert.NotNull(feature);
        Assert.Equal(context, feature.Context);
        Assert.Equal(feature, context.Request.GetHeaderParsing());
    }

    [Fact]
    public void TryParseHeader_parses_a_header()
    {
        var date = DateTimeOffset.UtcNow.ToString("R", CultureInfo.InvariantCulture);

        var context = CreateContext(new ServiceCollection().AddHeaderParsing());
        var dateHeaderKey = RegisterHeader(context, CommonHeaders.Date);
        context.Request.Headers["Date"] = date;

        Assert.True(context.Request.TryGetHeaderValue(dateHeaderKey, out var parsedDate, out var result));
        Assert.Equal(date, parsedDate.ToString("R", CultureInfo.InvariantCulture));
        Assert.Equal(ParsingResult.Success, result);

        Assert.True(context.Request.TryGetHeaderValue(dateHeaderKey, out parsedDate));
        Assert.Equal(date, parsedDate.ToString("R", CultureInfo.InvariantCulture));
        Assert.Equal(ParsingResult.Success, result);
    }

    private static HttpContext CreateContext(IServiceCollection? services = null)
    {
        services ??= new ServiceCollection();
        services.AddFakeLogging();

        return new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    private static HeaderKey<T> RegisterHeader<T>(HttpContext context, HeaderSetup<T> setup)
        where T : notnull
    {
        return context
            .RequestServices
            .GetRequiredService<IHeaderRegistry>()
            .Register(setup);
    }
}

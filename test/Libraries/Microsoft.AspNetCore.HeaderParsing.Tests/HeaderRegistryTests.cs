// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using Microsoft.AspNetCore.HeaderParsing.Parsers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.HeaderParsing.Test;

public class HeaderRegistryTests
{
    private readonly IOptions<HeaderParsingOptions> _options;
    private readonly IServiceCollection _services;

    public HeaderRegistryTests()
    {
        _options = Options.Create(new HeaderParsingOptions());
        _services = new ServiceCollection();
    }

    [Fact]
    public void Registers_header_without_default_value()
    {
        var registry = new HeaderRegistry(_services.BuildServiceProvider(), _options);

        var key = registry.Register(CommonHeaders.Date);

        Assert.Equal(0, key.Position);
        Assert.Equal(CommonHeaders.Date.ParserInstance, key.Parser);
        Assert.Equal(CommonHeaders.Date.HeaderName, key.Name);
        Assert.False(key.HasDefaultValue);
    }

    [Fact]
    public void Registers_header_with_typed_default_value()
    {
        var date = DateTimeOffset.Now.ToString("R", CultureInfo.InvariantCulture);

        _options.Value.DefaultValues.Add("Date", date);

        var registry = new HeaderRegistry(_services.BuildServiceProvider(), _options);

        var key = registry.Register(CommonHeaders.Date);

        Assert.True(key.HasDefaultValue);
        Assert.Equal(date, key.DefaultValue.ToString("R", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Registers_header_with_bad_default_value()
    {
        var date = "BOGUS";

        _options.Value.DefaultValues.Add("Date", date);

        var registry = new HeaderRegistry(_services.BuildServiceProvider(), _options);

        Assert.Throws<InvalidOperationException>(() => registry.Register(CommonHeaders.Date));
    }

    [Fact]
    public void Registers_multiple_headers_with_rising_index()
    {
        var registry = new HeaderRegistry(_services.BuildServiceProvider(), _options);

        var key1 = registry.Register(CommonHeaders.Date);
        var key2 = registry.Register(CommonHeaders.Accept);
        var key3 = registry.Register(CommonHeaders.AcceptLanguage);

        Assert.Equal(0, key1.Position);
        Assert.Equal(1, key2.Position);
        Assert.Equal(2, key3.Position);
    }

    [Fact]
    public void Registers_same_header_with_same_index()
    {
        var registry = new HeaderRegistry(_services.BuildServiceProvider(), _options);

        var key1 = registry.Register(CommonHeaders.Date);
        var key2 = registry.Register(CommonHeaders.Date);

        Assert.Equal(key1.Position, key2.Position);
    }

    [Fact]
    public void Registers_header_with_parser_type()
    {
        _services.AddSingleton(DateTimeOffsetParser.Instance);

        var registry = new HeaderRegistry(_services.BuildServiceProvider(), _options);

        var key = registry.Register(new HeaderSetup<DateTimeOffset>("MyDate", typeof(DateTimeOffsetParser)));

        Assert.IsType<DateTimeOffsetParser>(key.Parser);
    }
}

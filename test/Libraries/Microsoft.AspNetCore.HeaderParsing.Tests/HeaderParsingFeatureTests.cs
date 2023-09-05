// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using FluentAssertions;
using Microsoft.AspNetCore.HeaderParsing.Parsers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Telemetry.Metrics;
using Microsoft.Extensions.Telemetry.Testing.Logging;
using Microsoft.Extensions.Telemetry.Testing.Metrics;
using Microsoft.Net.Http.Headers;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.HeaderParsing.Test;

public sealed class HeaderParsingFeatureTests
{
    private readonly IOptions<HeaderParsingOptions> _options;
    private readonly IServiceCollection _services;
    private readonly FakeLogger<HeaderParsingFeature> _logger = new();
    private IHeaderRegistry? _registry;
    private HttpContext? _context;

    private IHeaderRegistry Registry => _registry ??= new HeaderRegistry(_services.BuildServiceProvider(), _options);
    private HttpContext Context => _context ??= new DefaultHttpContext { RequestServices = _services.BuildServiceProvider() };

    public HeaderParsingFeatureTests()
    {
        _options = Options.Create(new HeaderParsingOptions());
        _services = new ServiceCollection();
    }

    [Fact]
    public void Parses_header()
    {
        using var meter = new Meter<HeaderParsingFeature>();

        var date = DateTimeOffset.Now.ToString("R", CultureInfo.InvariantCulture);

        var key = Registry.Register(CommonHeaders.Date);
        Context.Request.Headers["Date"] = date;

        var feature = new HeaderParsingFeature(Registry, _logger, meter) { Context = Context };

        Assert.True(feature.TryGetHeaderValue(key, out var value, out var _));
        Assert.Equal(date, value.ToString("R", CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Parses_multiple_headers()
    {
        using var meter = new Meter<HeaderParsingFeature>();

        var currentDate = DateTimeOffset.Now.ToString("R", CultureInfo.InvariantCulture);
        var futureDate = DateTimeOffset.Now.AddHours(1).ToString("R", CultureInfo.InvariantCulture);

        var key = Registry.Register(CommonHeaders.Date);
        Context.Request.Headers["Date"] = currentDate;
        Context.Request.Headers["Test"] = futureDate;

        var feature = new HeaderParsingFeature(Registry, _logger, meter) { Context = Context };

        Assert.True(feature.TryGetHeaderValue(key, out var value, out var result));
        Assert.Equal(currentDate, value.ToString("R", CultureInfo.InvariantCulture));
        Assert.Equal(ParsingResult.Success, result);

        var key2 = Registry.Register(new HeaderSetup<DateTimeOffset>("Test", DateTimeOffsetParser.Instance));

        Assert.True(feature.TryGetHeaderValue(key2, out var textValue, out result));
        Assert.Equal(futureDate, textValue.ToString("R", CultureInfo.InvariantCulture));
        Assert.Equal(ParsingResult.Success, result);
    }

    [Fact]
    public void Parses_with_late_binding()
    {
        using var meter = new Meter<HeaderParsingFeature>();
        var date = DateTimeOffset.Now.ToString("R", CultureInfo.InvariantCulture);

        Context.Request.Headers["Date"] = date;

        var feature = new HeaderParsingFeature(Registry, _logger, meter) { Context = Context };

        var key = Registry.Register(CommonHeaders.Date);

        Assert.True(feature.TryGetHeaderValue(key, out var value, out var result));
        Assert.Equal(date, value.ToString("R", CultureInfo.InvariantCulture));
        Assert.Equal(ParsingResult.Success, result);
    }

    [Fact]
    public void TryParse_returns_false_on_header_not_found()
    {
        using var meter = new Meter<HeaderParsingFeature>();
        var feature = new HeaderParsingFeature(Registry, _logger, meter) { Context = Context };
        var key = Registry.Register(CommonHeaders.Date);

        Assert.False(feature.TryGetHeaderValue(key, out var value, out var _));

        Assert.Equal("Header 'Date' not found.", _logger.Collector.LatestRecord.Message);
    }

    [Fact]
    public void TryParse_returns_default_on_header_not_found()
    {
        using var meter = new Meter<HeaderParsingFeature>();

        var date = DateTimeOffset.Now.ToString("R", CultureInfo.InvariantCulture);
        _options.Value.DefaultValues.Add("Date", date);

        var feature = new HeaderParsingFeature(Registry, _logger, meter) { Context = Context };
        var key = Registry.Register(CommonHeaders.Date);

        Assert.True(feature.TryGetHeaderValue(key, out var value, out var result));
        Assert.Equal(date, value.ToString("R", CultureInfo.InvariantCulture));
        Assert.Equal(ParsingResult.Success, result);

        Assert.Equal("Using a default value for header 'Date'.", _logger.Collector.LatestRecord.Message);
    }

    [Fact]
    public void TryParse_returns_false_on_error()
    {
        using var meter = new Meter<HeaderParsingFeature>();
        using var metricCollector = new MetricCollector<long>(meter, @"HeaderParsing.ParsingErrors");
        Context.Request.Headers["Date"] = "Not a date.";

        var feature = new HeaderParsingFeature(Registry, _logger, meter) { Context = Context };
        var key = Registry.Register(CommonHeaders.Date);

        Assert.False(feature.TryGetHeaderValue(key, out var value, out var result));
        Assert.Equal(default, value);
        Assert.Equal(ParsingResult.Error, result);

        Assert.Equal("Can't parse header 'Date' due to 'Unable to parse date time offset value.'.", _logger.Collector.LatestRecord.Message);

        var latest = metricCollector.LastMeasurement!;
        latest.Value.Should().Be(1);
        latest.Tags["HeaderName"].Should().Be("Date");
        latest.Tags["Kind"].Should().Be("Unable to parse date time offset value.");
    }

    [Fact]
    public void Dispose_resets_state_and_returns_to_pool()
    {
        using var meter = new Meter<HeaderParsingFeature>();

        var pool = new Mock<ObjectPool<HeaderParsingFeature.PoolHelper>>(MockBehavior.Strict);
        var helper = new HeaderParsingFeature.PoolHelper(pool.Object, Registry, _logger, meter);
        helper.Feature.Context = Context;
        pool.Setup(x => x.Return(helper));

        var firstHeaderKey = Registry.Register(new HeaderSetup<DateTimeOffset>("FirstHeader", DateTimeOffsetParser.Instance));
        var secondHeaderKey = Registry.Register(new HeaderSetup<DateTimeOffset>("SecondHeader", DateTimeOffsetParser.Instance));
        var thirdHeaderKey = Registry.Register(new HeaderSetup<DateTimeOffset>("ThirdHeader", DateTimeOffsetParser.Instance));

        Assert.False(helper.Feature.TryGetHeaderValue(firstHeaderKey, out _));
        Assert.False(helper.Feature.TryGetHeaderValue(thirdHeaderKey, out _));

        helper.Dispose();
        Assert.Null(helper.Feature.Context);

        Context.Request.Headers[firstHeaderKey.Name] = DateTimeOffset.Now.ToString("R", CultureInfo.InvariantCulture);
        Context.Request.Headers[thirdHeaderKey.Name] = DateTimeOffset.Now.ToString("R", CultureInfo.InvariantCulture);

        helper.Feature.Context = Context;

        Assert.True(helper.Feature.TryGetHeaderValue(firstHeaderKey, out _));
        Assert.True(helper.Feature.TryGetHeaderValue(thirdHeaderKey, out _));

        pool.VerifyAll();
    }

    [Fact]
    public void CachingWorks()
    {
        using var meter = new Meter<HeaderParsingFeature>();
        using var metricCollector = new MetricCollector<long>(meter, @"HeaderParsing.CacheAccess");

        Context.Request.Headers[HeaderNames.CacheControl] = "max-age=604800";

        var feature = new HeaderParsingFeature(Registry, _logger, meter) { Context = Context };
        var feature2 = new HeaderParsingFeature(Registry, _logger, meter) { Context = Context };
        var key = Registry.Register(CommonHeaders.CacheControl);

        Assert.True(feature.TryGetHeaderValue(key, out var value1, out var error1));
        Assert.True(feature.TryGetHeaderValue(key, out var value2, out var error2));
        Assert.True(feature2.TryGetHeaderValue(key, out var value3, out var error3));
        Assert.Same(value1, value2);
        Assert.Same(value1, value3);

        var latest = metricCollector.LastMeasurement!;
        latest.Value.Should().Be(1);
        latest.Tags["HeaderName"].Should().Be(HeaderNames.CacheControl);
        latest.Tags["Type"].Should().Be("Hit");
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.HeaderParsing.Parsers;
using Microsoft.Extensions.Primitives;
using Xunit;
using UriParser = Microsoft.AspNetCore.HeaderParsing.Parsers.UriParser;

namespace Microsoft.AspNetCore.HeaderParsing.Test;

public class ParserTests
{
    [Fact]
    public void Host_ReturnsParsedValue()
    {
        var sv = new StringValues("web.vortex.data.microsoft.com");
        Assert.True(HostHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Equal("web.vortex.data.microsoft.com", result.Host);
        Assert.Null(result.Port);
        Assert.Null(error);
    }

    [Fact]
    public void Host_Multi()
    {
        var sv = new StringValues(new[] { "Hello", "World" });
        Assert.False(HostHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Date_ReturnsParsedValue()
    {
        var sv = new StringValues("Wed, 21 Oct 2015 07:28:14 GMT");
        Assert.True(DateTimeOffsetParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Equal(DayOfWeek.Wednesday, result.DayOfWeek);
        Assert.Equal(21, result.Day);
        Assert.Equal(10, result.Month);
        Assert.Equal(2015, result.Year);
        Assert.Equal(7, result.Hour);
        Assert.Equal(28, result.Minute);
        Assert.Equal(14, result.Second);
        Assert.Null(error);
    }

    [Fact]
    public void Date_ReturnsNullForEmptyValue()
    {
        var sv = new StringValues(string.Empty);
        Assert.False(DateTimeOffsetParser.Instance.TryParse(sv, out var result, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Date_ReturnsNullForInvalidValue()
    {
        var sv = new StringValues("Hello World");
        Assert.False(DateTimeOffsetParser.Instance.TryParse(sv, out var result, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Date_Multi()
    {
        var sv = new StringValues(new[] { "Hello", "World" });
        Assert.False(DateTimeOffsetParser.Instance.TryParse(sv, out var result, out var error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Cookkie_ReturnsParsedValue()
    {
        var sv = new StringValues("csrftoken=u32t4o3tb3gg43");
        Assert.True(CookieHeaderValueListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Single(result);
        Assert.Equal("csrftoken", result[0].Name.Value);
        Assert.Equal("u32t4o3tb3gg43", result[0].Value.Value);
        Assert.Null(error);
    }

    [Fact]
    public void Cookie_ReturnsMultipleCookiesForMultipleCookies()
    {
        var sv = new StringValues("csrftoken=u32t4o3tb3gg43; _gat=1");
        Assert.True(CookieHeaderValueListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Equal(2, result.Count);
        Assert.Equal("csrftoken", result[0].Name.Value);
        Assert.Equal("u32t4o3tb3gg43", result[0].Value.Value);
        Assert.Equal("_gat", result[1].Name.Value);
        Assert.Equal("1", result[1].Value.Value);
        Assert.Null(error);
    }

    [Fact]
    public void Cookie_ReturnsNullForEmptyValue()
    {
        var sv = new StringValues(string.Empty);
        Assert.False(CookieHeaderValueListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Cookie_ReturnsNullForInvalidValue()
    {
        var sv = new StringValues("HelloWorld");
        Assert.False(CookieHeaderValueListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void CacheControl_ReturnsParsedValue()
    {
        var sv = new StringValues("public, max-age=604800");
        Assert.True(CacheControlHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.True(result.Public);
        Assert.Equal(TimeSpan.FromSeconds(604800), result.MaxAge);
        Assert.Null(error);
    }

    [Fact]
    public void CacheControl_ReturnsNullWhenInvalid()
    {
        var sv = new StringValues("ZZZ=ZZZ=ZZZ=ZZZ");
        Assert.False(CacheControlHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void CacheControl_Multi()
    {
        var sv = new StringValues(new[] { "Hello", "World" });
        Assert.False(CacheControlHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void ContentDisposition_ReturnsParsedValue()
    {
        var sv = new StringValues("attachment; filename=\"cool.html\"");
        Assert.True(ContentDispositionHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Equal("cool.html", result.FileName);
        Assert.Equal("attachment", result.DispositionType);
        Assert.Null(error);
    }

    [Fact]
    public void ContentDisposition_Multi()
    {
        var sv = new StringValues(new[] { "attachment; filename=\"cool.html\"", "attachment; filename=\"cool.html\"" });
        Assert.False(ContentDispositionHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void ContentDisposition_ReturnsNullWhenInvalid()
    {
        var sv = new StringValues("zz=zz=zz");
        Assert.False(ContentDispositionHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void MediaType_ReturnsParsedValue()
    {
        var sv = new StringValues("text/html; charset=UTF-8");
        Assert.True(MediaTypeHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Equal("text/html", result.MediaType);
        Assert.Equal("UTF-8", result.Charset);
        Assert.Null(error);
    }

    [Fact]
    public void MediaType_Multi()
    {
        var sv = new StringValues(new[] { "text/html; charset=UTF-8", "text/html; charset=UTF-8" });
        Assert.False(MediaTypeHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void MediaType_ReturnsNullWhenInvalid()
    {
        var sv = new StringValues(string.Empty);
        Assert.False(MediaTypeHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void MediaTypes_ReturnsParsedValue()
    {
        var sv = new StringValues("text/html; charset=UTF-8");
        Assert.True(MediaTypeHeaderValueListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Single(result);
        Assert.Equal("text/html", result[0].MediaType);
        Assert.Equal("UTF-8", result[0].Charset);
        Assert.Null(error);
    }

    [Fact]
    public void MediaTypes_ReturnsNullWhenInvalid()
    {
        var sv = new StringValues(string.Empty);
        Assert.False(MediaTypeHeaderValueListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void EntityTag_ReturnsParsedValue()
    {
        var sv = new StringValues("\"HelloWorld\"");
        Assert.True(EntityTagHeaderValueListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Single(result!);
        Assert.Equal("\"HelloWorld\"", result[0].Tag);
        Assert.Null(error);
    }

    [Fact]
    public void EntityTag_ReturnsNullWhenInvalid()
    {
        var sv = new StringValues(string.Empty);
        Assert.False(EntityTagHeaderValueListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void StringQuality_ReturnsParsedValue()
    {
        var sv = new StringValues("en-US");
        Assert.True(StringWithQualityHeaderValueListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Single(result!);
        Assert.Equal("en-US", result[0].Value);
        Assert.Null(error);
    }

    [Fact]
    public void StringQuality_Multi()
    {
        var sv = new StringValues("en-US,en;q=0.5");
        Assert.True(StringWithQualityHeaderValueListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Equal(2, result.Count);
        Assert.Equal("en-US", result[0].Value);
        Assert.Equal("en", result[1].Value);
        Assert.Equal(0.5, result[1].Quality);
        Assert.Null(error);
    }

    [Fact]
    public void StringQuality_ReturnsNullWhenInvalid()
    {
        var sv = new StringValues(string.Empty);
        Assert.False(StringWithQualityHeaderValueListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Uri_ReturnsParsedValue()
    {
        var sv = new StringValues("https://foo.com:81");
        Assert.True(UriParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Equal("foo.com", result!.Host);
        Assert.Equal(81, result.Port);
        Assert.Null(error);
    }

    [Fact]
    public void Uri_Multi()
    {
        var sv = new StringValues(new[] { "http://foo.com", "http://bar.com" });
        Assert.False(UriParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Uri_ReturnsNullWhenInvalid()
    {
        var sv = new StringValues("http://localhost:XXX");
        Assert.False(UriParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Range_ReturnsParsedValue()
    {
        var sv = new StringValues("bytes=200-1000");
        Assert.True(RangeHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Equal("bytes", result!.Unit);
        Assert.Single(result.Ranges);
        Assert.Equal(200, result.Ranges.Single().From);
        Assert.Equal(1000, result.Ranges.Single().To);
        Assert.Null(error);
    }

    [Fact]
    public void Range_Multi()
    {
        var sv = new StringValues(new[] { "bytes=200-1000", "bytes=3000-4000" });
        Assert.False(RangeHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void Range_ReturnsNullWhenInvalid()
    {
        var sv = new StringValues("Hello World");
        Assert.False(RangeHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void RangeCondition_ReturnsParsedValue()
    {
        var sv = new StringValues("Wed, 21 Oct 2015 07:28:14 GMT");
        Assert.True(RangeConditionHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Equal(DayOfWeek.Wednesday, result!.LastModified!.Value.DayOfWeek);
        Assert.Equal(21, result!.LastModified.Value.Day);
        Assert.Equal(10, result!.LastModified.Value.Month);
        Assert.Equal(2015, result!.LastModified.Value.Year);
        Assert.Equal(7, result!.LastModified.Value.Hour);
        Assert.Equal(28, result!.LastModified!.Value.Minute);
        Assert.Equal(14, result!.LastModified.Value.Second);
        Assert.Null(error);

        sv = new StringValues("\"67ab43\"");
        Assert.True(RangeConditionHeaderValueParser.Instance.TryParse(sv, out result, out error));
        Assert.Equal("\"67ab43\"", result!.EntityTag!.Tag);
        Assert.Null(error);
    }

    [Fact]
    public void RangeCondition_Multi()
    {
        var sv = new StringValues(new[] { "\"67ab43\"", "\"67ab43\"" });
        Assert.False(RangeConditionHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void RangeCondition_ReturnsNullWhenInvalid()
    {
        var sv = new StringValues("Hello World");
        Assert.False(RangeConditionHeaderValueParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void IpAddressesList_WithSpaces_ReturnsParsedValue()
    {
        var sv = new StringValues(new[] { "    1.1.1.1    ,    192.168.1.100    ", "    3.3.3.3    " });
        Assert.True(IPAddressListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.NotNull(result);
        Assert.Equal(new List<IPAddress> { IPAddress.Parse("1.1.1.1"), IPAddress.Parse("192.168.1.100"), IPAddress.Parse("3.3.3.3") }, result);
        Assert.Null(error);
    }

    [Fact]
    public void IpAddressesList_NoSpaces_ReturnsParsedValue()
    {
        var sv = new StringValues(new[] { "1.1.1.1,192.168.1.100", "3.3.3.3" });
        Assert.True(IPAddressListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.NotNull(result);
        Assert.Equal(new List<IPAddress> { IPAddress.Parse("1.1.1.1"), IPAddress.Parse("192.168.1.100"), IPAddress.Parse("3.3.3.3") }, result);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("      1.1.1.1")]
    [InlineData("1.1.1.1")]
    public void IpAddressesList_SingleIpV4_ReturnsParsedValue(string testValue)
    {
        var sv = new StringValues(new[] { testValue });
        Assert.True(IPAddressListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.NotNull(result);
        Assert.Equal(new List<IPAddress> { IPAddress.Parse("1.1.1.1") }, result);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("      1::ffff")]
    [InlineData("1::ffff")]
    public void IpAddressesList_SingleIpV6_ReturnsParsedValue(string testValue)
    {
        var sv = new StringValues(new[] { testValue });
        Assert.True(IPAddressListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.NotNull(result);
        Assert.Equal(new List<IPAddress> { IPAddress.Parse("1::ffff") }, result);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("1.1.1.1, 2.2.2.2", "a.b.c.d")]
    [InlineData("1.1.1.1,,2.2.2.2")]
    [InlineData("1.1.1.1,")]
    public void IpAddressesList_Malformed_ReturnsNull(params string[] values)
    {
        var sv = new StringValues(values);
        Assert.False(IPAddressListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Fact]
    public void IpAddressesList_FirstEmptyIp_ReturnsErrorAboutEmptyIp()
    {
        var sv = new StringValues(",1.1.1.1");
        Assert.False(IPAddressListParser.Instance.TryParse(sv, out var result, out var error));
        Assert.Null(result);
        Assert.Equal("IP address cannot be empty.", error);
    }
}


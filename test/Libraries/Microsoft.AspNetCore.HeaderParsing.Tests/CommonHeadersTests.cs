// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.HeaderParsing.Parsers;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.HeaderParsing.Test;

public class CommonHeadersTests
{
    [Fact]
    public void Host_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.Host, CommonHeaders.Host.HeaderName);
        Assert.Equal(HostHeaderValueParser.Instance, CommonHeaders.Host.ParserInstance);
        Assert.Null(CommonHeaders.Host.ParserType);
    }

    [Fact]
    public void Accept_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.Accept, CommonHeaders.Accept.HeaderName);
        Assert.Equal(MediaTypeHeaderValueListParser.Instance, CommonHeaders.Accept.ParserInstance);
        Assert.Null(CommonHeaders.Accept.ParserType);
    }

    [Fact]
    public void AcceptEncoding_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.AcceptEncoding, CommonHeaders.AcceptEncoding.HeaderName);
        Assert.Equal(StringWithQualityHeaderValueListParser.Instance, CommonHeaders.AcceptEncoding.ParserInstance);
        Assert.Null(CommonHeaders.AcceptEncoding.ParserType);
    }

    [Fact]
    public void AcceptLanguage_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.AcceptLanguage, CommonHeaders.AcceptLanguage.HeaderName);
        Assert.Equal(StringWithQualityHeaderValueListParser.Instance, CommonHeaders.AcceptLanguage.ParserInstance);
        Assert.Null(CommonHeaders.AcceptLanguage.ParserType);
    }

    [Fact]
    public void CacheControl_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.CacheControl, CommonHeaders.CacheControl.HeaderName);
        Assert.Equal(CacheControlHeaderValueParser.Instance, CommonHeaders.CacheControl.ParserInstance);
        Assert.Null(CommonHeaders.CacheControl.ParserType);
    }

    [Fact]
    public void ContentDisposition_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.ContentDisposition, CommonHeaders.ContentDisposition.HeaderName);
        Assert.Equal(ContentDispositionHeaderValueParser.Instance, CommonHeaders.ContentDisposition.ParserInstance);
        Assert.Null(CommonHeaders.ContentDisposition.ParserType);
    }

    [Fact]
    public void ContentType_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.ContentType, CommonHeaders.ContentType.HeaderName);
        Assert.Equal(MediaTypeHeaderValueParser.Instance, CommonHeaders.ContentType.ParserInstance);
        Assert.Null(CommonHeaders.ContentType.ParserType);
    }

    [Fact]
    public void Cookie_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.Cookie, CommonHeaders.Cookie.HeaderName);
        Assert.Equal(CookieHeaderValueListParser.Instance, CommonHeaders.Cookie.ParserInstance);
        Assert.Null(CommonHeaders.Cookie.ParserType);
    }

    [Fact]
    public void Date_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.Date, CommonHeaders.Date.HeaderName);
        Assert.Equal(DateTimeOffsetParser.Instance, CommonHeaders.Date.ParserInstance);
        Assert.Null(CommonHeaders.Date.ParserType);
    }

    [Fact]
    public void IfMatch_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.IfMatch, CommonHeaders.IfMatch.HeaderName);
        Assert.Equal(EntityTagHeaderValueListParser.Instance, CommonHeaders.IfMatch.ParserInstance);
        Assert.Null(CommonHeaders.IfMatch.ParserType);
    }

    [Fact]
    public void IfModifiedSince_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.IfModifiedSince, CommonHeaders.IfModifiedSince.HeaderName);
        Assert.Equal(EntityTagHeaderValueListParser.Instance, CommonHeaders.IfModifiedSince.ParserInstance);
        Assert.Null(CommonHeaders.IfModifiedSince.ParserType);
    }

    [Fact]
    public void IfNoneMatch_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.IfNoneMatch, CommonHeaders.IfNoneMatch.HeaderName);
        Assert.Equal(EntityTagHeaderValueListParser.Instance, CommonHeaders.IfNoneMatch.ParserInstance);
        Assert.Null(CommonHeaders.IfNoneMatch.ParserType);
    }

    [Fact]
    public void IfRange_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.IfRange, CommonHeaders.IfRange.HeaderName);
        Assert.Equal(RangeConditionHeaderValueParser.Instance, CommonHeaders.IfRange.ParserInstance);
        Assert.Null(CommonHeaders.IfRange.ParserType);
    }

    [Fact]
    public void IfUnmodifiedSince_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.IfUnmodifiedSince, CommonHeaders.IfUnmodifiedSince.HeaderName);
        Assert.Equal(DateTimeOffsetParser.Instance, CommonHeaders.IfUnmodifiedSince.ParserInstance);
        Assert.Null(CommonHeaders.IfUnmodifiedSince.ParserType);
    }

    [Fact]
    public void Range_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.Range, CommonHeaders.Range.HeaderName);
        Assert.Equal(RangeHeaderValueParser.Instance, CommonHeaders.Range.ParserInstance);
        Assert.Null(CommonHeaders.Range.ParserType);
    }

    [Fact]
    public void Referer_header_has_correct_setup()
    {
        Assert.Equal(HeaderNames.Referer, CommonHeaders.Referer.HeaderName);
        Assert.Equal(UriParser.Instance, CommonHeaders.Referer.ParserInstance);
        Assert.Null(CommonHeaders.Referer.ParserType);
    }

    [Fact]
    public void XForwardedFor_header_has_correct_setup()
    {
        Assert.Equal("X-Forwarded-For", CommonHeaders.XForwardedFor.HeaderName);
        Assert.Equal(IPAddressListParser.Instance, CommonHeaders.XForwardedFor.ParserInstance);
        Assert.Null(CommonHeaders.XForwardedFor.ParserType);
    }
}

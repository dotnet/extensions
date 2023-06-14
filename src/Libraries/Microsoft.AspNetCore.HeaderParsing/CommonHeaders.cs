// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.HeaderParsing.Parsers;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Common header setups.
/// </summary>
public static class CommonHeaders
{
    /// <summary>
    /// Gets Host header setup.
    /// </summary>
    public static HeaderSetup<HostHeaderValue> Host => new(HeaderNames.Host, HostHeaderValueParser.Instance);

    /// <summary>
    /// Gets Accept header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<MediaTypeHeaderValue>> Accept => new(HeaderNames.Accept, MediaTypeHeaderValueListParser.Instance);

    /// <summary>
    /// Gets AcceptEncoding header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<StringWithQualityHeaderValue>> AcceptEncoding => new(HeaderNames.AcceptEncoding, StringWithQualityHeaderValueListParser.Instance, cacheable: true);

    /// <summary>
    /// Gets AcceptLanguage header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<StringWithQualityHeaderValue>> AcceptLanguage => new(HeaderNames.AcceptLanguage, StringWithQualityHeaderValueListParser.Instance, cacheable: true);

    /// <summary>
    /// Gets CacheControl header setup.
    /// </summary>
    public static HeaderSetup<CacheControlHeaderValue> CacheControl => new(HeaderNames.CacheControl, CacheControlHeaderValueParser.Instance, cacheable: true);

    /// <summary>
    /// Gets ContentDisposition header setup.
    /// </summary>
    public static HeaderSetup<ContentDispositionHeaderValue> ContentDisposition => new(HeaderNames.ContentDisposition, ContentDispositionHeaderValueParser.Instance, cacheable: true);

    /// <summary>
    /// Gets ContentType header setup.
    /// </summary>
    public static HeaderSetup<MediaTypeHeaderValue> ContentType => new(HeaderNames.ContentType, MediaTypeHeaderValueParser.Instance, cacheable: true);

    /// <summary>
    /// Gets Cookie header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<CookieHeaderValue>> Cookie => new(HeaderNames.Cookie, CookieHeaderValueListParser.Instance);

    /// <summary>
    /// Gets Date header setup.
    /// </summary>
    public static HeaderSetup<DateTimeOffset> Date => new(HeaderNames.Date, DateTimeOffsetParser.Instance);

    /// <summary>
    /// Gets IfMatch header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<EntityTagHeaderValue>> IfMatch => new(HeaderNames.IfMatch, EntityTagHeaderValueListParser.Instance);

    /// <summary>
    /// Gets IfModifiedSince header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<EntityTagHeaderValue>> IfModifiedSince => new(HeaderNames.IfModifiedSince, EntityTagHeaderValueListParser.Instance);

    /// <summary>
    /// Gets IfNoneMatch header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<EntityTagHeaderValue>> IfNoneMatch => new(HeaderNames.IfNoneMatch, EntityTagHeaderValueListParser.Instance);

    /// <summary>
    /// Gets IfRange header setup.
    /// </summary>
    public static HeaderSetup<RangeConditionHeaderValue> IfRange => new(HeaderNames.IfRange, RangeConditionHeaderValueParser.Instance);

    /// <summary>
    /// Gets IfUnmodifiedSince header setup.
    /// </summary>
    public static HeaderSetup<DateTimeOffset> IfUnmodifiedSince => new(HeaderNames.IfUnmodifiedSince, DateTimeOffsetParser.Instance);

    /// <summary>
    /// Gets Range header setup.
    /// </summary>
    public static HeaderSetup<RangeHeaderValue> Range => new(HeaderNames.Range, RangeHeaderValueParser.Instance);

    /// <summary>
    /// Gets Referrer header setup.
    /// </summary>
    public static HeaderSetup<Uri> Referer => new(HeaderNames.Referer, Parsers.UriParser.Instance, cacheable: true);

    /// <summary>
    /// Gets XForwardedFor header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<IPAddress>> XForwardedFor => new("X-Forwarded-For", IPAddressListParser.Instance);
}

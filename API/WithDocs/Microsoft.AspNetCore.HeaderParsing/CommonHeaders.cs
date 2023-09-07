// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using System.Collections.Generic;
using System.Net;
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
    public static HeaderSetup<HostHeaderValue> Host { get; }

    /// <summary>
    /// Gets Accept header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<MediaTypeHeaderValue>> Accept { get; }

    /// <summary>
    /// Gets AcceptEncoding header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<StringWithQualityHeaderValue>> AcceptEncoding { get; }

    /// <summary>
    /// Gets AcceptLanguage header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<StringWithQualityHeaderValue>> AcceptLanguage { get; }

    /// <summary>
    /// Gets CacheControl header setup.
    /// </summary>
    public static HeaderSetup<CacheControlHeaderValue> CacheControl { get; }

    /// <summary>
    /// Gets ContentDisposition header setup.
    /// </summary>
    public static HeaderSetup<ContentDispositionHeaderValue> ContentDisposition { get; }

    /// <summary>
    /// Gets ContentType header setup.
    /// </summary>
    public static HeaderSetup<MediaTypeHeaderValue> ContentType { get; }

    /// <summary>
    /// Gets Cookie header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<CookieHeaderValue>> Cookie { get; }

    /// <summary>
    /// Gets Date header setup.
    /// </summary>
    public static HeaderSetup<DateTimeOffset> Date { get; }

    /// <summary>
    /// Gets IfMatch header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<EntityTagHeaderValue>> IfMatch { get; }

    /// <summary>
    /// Gets IfModifiedSince header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<EntityTagHeaderValue>> IfModifiedSince { get; }

    /// <summary>
    /// Gets IfNoneMatch header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<EntityTagHeaderValue>> IfNoneMatch { get; }

    /// <summary>
    /// Gets IfRange header setup.
    /// </summary>
    public static HeaderSetup<RangeConditionHeaderValue> IfRange { get; }

    /// <summary>
    /// Gets IfUnmodifiedSince header setup.
    /// </summary>
    public static HeaderSetup<DateTimeOffset> IfUnmodifiedSince { get; }

    /// <summary>
    /// Gets Range header setup.
    /// </summary>
    public static HeaderSetup<RangeHeaderValue> Range { get; }

    /// <summary>
    /// Gets Referrer header setup.
    /// </summary>
    public static HeaderSetup<Uri> Referer { get; }

    /// <summary>
    /// Gets XForwardedFor header setup.
    /// </summary>
    public static HeaderSetup<IReadOnlyList<IPAddress>> XForwardedFor { get; }
}

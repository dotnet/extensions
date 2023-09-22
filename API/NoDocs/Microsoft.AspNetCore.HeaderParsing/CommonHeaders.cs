// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.HeaderParsing;

public static class CommonHeaders
{
    public static HeaderSetup<HostHeaderValue> Host { get; }
    public static HeaderSetup<IReadOnlyList<MediaTypeHeaderValue>> Accept { get; }
    public static HeaderSetup<IReadOnlyList<StringWithQualityHeaderValue>> AcceptEncoding { get; }
    public static HeaderSetup<IReadOnlyList<StringWithQualityHeaderValue>> AcceptLanguage { get; }
    public static HeaderSetup<CacheControlHeaderValue> CacheControl { get; }
    public static HeaderSetup<ContentDispositionHeaderValue> ContentDisposition { get; }
    public static HeaderSetup<MediaTypeHeaderValue> ContentType { get; }
    public static HeaderSetup<IReadOnlyList<CookieHeaderValue>> Cookie { get; }
    public static HeaderSetup<DateTimeOffset> Date { get; }
    public static HeaderSetup<IReadOnlyList<EntityTagHeaderValue>> IfMatch { get; }
    public static HeaderSetup<IReadOnlyList<EntityTagHeaderValue>> IfModifiedSince { get; }
    public static HeaderSetup<IReadOnlyList<EntityTagHeaderValue>> IfNoneMatch { get; }
    public static HeaderSetup<RangeConditionHeaderValue> IfRange { get; }
    public static HeaderSetup<DateTimeOffset> IfUnmodifiedSince { get; }
    public static HeaderSetup<RangeHeaderValue> Range { get; }
    public static HeaderSetup<Uri> Referer { get; }
    public static HeaderSetup<IReadOnlyList<IPAddress>> XForwardedFor { get; }
}

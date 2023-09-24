// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.HeaderParsing;

namespace Microsoft.AspNetCore.Http;

public static class HeaderParsingHttpRequestExtensions
{
    public static HeaderParsingFeature GetHeaderParsing(this HttpRequest request);
    public static bool TryGetHeaderValue<T>(this HttpRequest request, HeaderKey<T> header, [NotNullWhen(true)] out T? value) where T : notnull;
    public static bool TryGetHeaderValue<T>(this HttpRequest request, HeaderKey<T> header, [NotNullWhen(true)] out T? value, out ParsingResult result) where T : notnull;
}

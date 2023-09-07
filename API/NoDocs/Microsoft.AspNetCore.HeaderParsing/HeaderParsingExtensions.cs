// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.HeaderParsing;

public static class HeaderParsingExtensions
{
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services);
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services, Action<HeaderParsingOptions> configuration);
    public static IServiceCollection AddHeaderParsing(this IServiceCollection services, IConfigurationSection section);
    public static HeaderParsingFeature GetHeaderParsing(this HttpRequest request);
    public static bool TryGetHeaderValue<T>(this HttpRequest request, HeaderKey<T> header, [NotNullWhen(true)] out T? value) where T : notnull;
    public static bool TryGetHeaderValue<T>(this HttpRequest request, HeaderKey<T> header, [NotNullWhen(true)] out T? value, out ParsingResult result) where T : notnull;
}

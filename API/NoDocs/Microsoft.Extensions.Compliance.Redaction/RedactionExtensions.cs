// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Compliance.Redaction;

public static class RedactionExtensions
{
    public static IHostBuilder ConfigureRedaction(this IHostBuilder builder);
    public static IHostBuilder ConfigureRedaction(this IHostBuilder builder, Action<HostBuilderContext, IRedactionBuilder> configure);
    public static IHostBuilder ConfigureRedaction(this IHostBuilder builder, Action<IRedactionBuilder> configure);
    public static IServiceCollection AddRedaction(this IServiceCollection services);
    public static IServiceCollection AddRedaction(this IServiceCollection services, Action<IRedactionBuilder> configure);
    public static IRedactionBuilder SetXXHash3Redactor(this IRedactionBuilder builder, Action<XXHash3RedactorOptions> configure, params DataClassification[] classifications);
    public static IRedactionBuilder SetXXHash3Redactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassification[] classifications);
}

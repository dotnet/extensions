// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Compliance.Testing;

public static class FakeRedactionExtensions
{
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, params DataClassification[] classifications);
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, Action<FakeRedactorOptions> configure, params DataClassification[] classifications);
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassification[] classifications);
    public static IServiceCollection AddFakeRedaction(this IServiceCollection services);
    public static IServiceCollection AddFakeRedaction(this IServiceCollection services, Action<FakeRedactorOptions> configure);
    public static FakeRedactionCollector GetFakeRedactionCollector(this IServiceProvider serviceProvider);
}

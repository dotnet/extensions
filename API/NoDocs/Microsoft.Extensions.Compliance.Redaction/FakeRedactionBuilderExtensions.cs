// Assembly 'Microsoft.Extensions.Compliance.Testing'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Compliance.Redaction;

public static class FakeRedactionBuilderExtensions
{
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, params DataClassification[] classifications);
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, Action<FakeRedactorOptions> configure, params DataClassification[] classifications);
    public static IRedactionBuilder SetFakeRedactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassification[] classifications);
}

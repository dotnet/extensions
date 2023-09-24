// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Compliance.Redaction;

public static class RedactionBuilderExtensions
{
    public static IRedactionBuilder SetXxHash3Redactor(this IRedactionBuilder builder, Action<XxHash3RedactorOptions> configure, params DataClassification[] classifications);
    public static IRedactionBuilder SetXxHash3Redactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassification[] classifications);
}

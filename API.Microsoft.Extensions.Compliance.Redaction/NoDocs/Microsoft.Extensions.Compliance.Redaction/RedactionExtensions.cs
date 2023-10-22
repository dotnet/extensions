// Assembly 'Microsoft.Extensions.Compliance.Redaction'

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.Compliance.Redaction;

public static class RedactionExtensions
{
    [Experimental("EXTEXP0002", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IRedactionBuilder SetHmacRedactor(this IRedactionBuilder builder, Action<HmacRedactorOptions> configure, params DataClassificationSet[] classifications);
    [Experimental("EXTEXP0002", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
    public static IRedactionBuilder SetHmacRedactor(this IRedactionBuilder builder, IConfigurationSection section, params DataClassificationSet[] classifications);
}

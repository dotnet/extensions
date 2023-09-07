// Assembly 'Microsoft.Extensions.Telemetry.Abstractions'

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Telemetry.Logging;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
[Experimental("EXTEXP0003", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public sealed class TagProviderAttribute : Attribute
{
    public Type ProviderType { get; }
    public string ProviderMethod { get; }
    public bool OmitReferenceName { get; set; }
    public TagProviderAttribute(Type providerType, string providerMethod);
}

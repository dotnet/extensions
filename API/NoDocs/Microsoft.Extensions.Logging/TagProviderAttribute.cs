// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Logging;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class TagProviderAttribute : Attribute
{
    public Type ProviderType { get; }
    public string ProviderMethod { get; }
    public bool OmitReferenceName { get; set; }
    public TagProviderAttribute(Type providerType, string providerMethod);
}

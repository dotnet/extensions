// Assembly 'Microsoft.Extensions.EnumStrings'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.EnumStrings;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Enum, AllowMultiple = true)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class EnumStringsAttribute : Attribute
{
    public Type? EnumType { get; }
    public string? ExtensionNamespace { get; set; }
    public string? ExtensionClassName { get; set; }
    public string ExtensionMethodName { get; set; }
    public string ExtensionClassModifiers { get; set; }
    public EnumStringsAttribute();
    public EnumStringsAttribute(Type enumType);
}

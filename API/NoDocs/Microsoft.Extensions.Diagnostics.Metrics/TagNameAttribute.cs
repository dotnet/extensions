// Assembly 'Microsoft.Extensions.Diagnostics.ExtraAbstractions'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Diagnostics.Metrics;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class TagNameAttribute : Attribute
{
    public string Name { get; }
    public TagNameAttribute(string name);
}

// Assembly 'Microsoft.Extensions.Options.Contextual'

using System;
using System.Diagnostics;

namespace Microsoft.Extensions.Options.Contextual;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class OptionsContextAttribute : Attribute
{
    public OptionsContextAttribute();
}

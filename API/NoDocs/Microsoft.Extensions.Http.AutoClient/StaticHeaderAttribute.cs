// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.AutoClient;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class StaticHeaderAttribute : Attribute
{
    public string Header { get; }
    public string Value { get; }
    public StaticHeaderAttribute(string header, string value);
}

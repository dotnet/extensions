// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.AutoClient;

[AttributeUsage(AttributeTargets.Parameter)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class QueryAttribute : Attribute
{
    public string? Key { get; }
    public QueryAttribute();
    public QueryAttribute(string key);
}

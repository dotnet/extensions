// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.AutoClient;

[AttributeUsage(AttributeTargets.Interface)]
[Conditional("CODE_GENERATION_ATTRIBUTES")]
public sealed class AutoClientAttribute : Attribute
{
    public string HttpClientName { get; }
    public string? CustomDependencyName { get; }
    public AutoClientAttribute(string httpClientName);
    public AutoClientAttribute(string httpClientName, string customDependencyName);
}

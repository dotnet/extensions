// Assembly 'Microsoft.Extensions.Http.AutoClient'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Http.AutoClient;

[AttributeUsage(AttributeTargets.Method)]
public sealed class PutAttribute : Attribute
{
    public string Path { get; }
    public string? RequestName { get; set; }
    public PutAttribute(string path);
}

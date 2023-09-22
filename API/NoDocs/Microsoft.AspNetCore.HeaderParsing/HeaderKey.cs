// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderParsing;

public sealed class HeaderKey<T> where T : notnull
{
    public string Name { get; }
    public override string ToString();
}

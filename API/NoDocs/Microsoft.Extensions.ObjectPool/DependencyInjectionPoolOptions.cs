// Assembly 'Microsoft.Extensions.ObjectPool.DependencyInjection'

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.ObjectPool;

[Experimental("EXTEXP0010", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
public sealed class DependencyInjectionPoolOptions
{
    public int Capacity { get; set; }
    public DependencyInjectionPoolOptions();
}

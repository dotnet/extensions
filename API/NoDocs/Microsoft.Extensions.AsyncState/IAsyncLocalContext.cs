// Assembly 'Microsoft.Extensions.AsyncState'

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AsyncState;

[Experimental("EXTEXP0006", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IAsyncLocalContext<T> : IAsyncContext<T> where T : class
{
}

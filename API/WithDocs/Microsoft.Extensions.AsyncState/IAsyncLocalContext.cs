// Assembly 'Microsoft.Extensions.AsyncState'

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.AsyncState;

/// <summary>
/// Provides access to the current async context stored outside of the HTTP pipeline.
/// </summary>
/// <typeparam name="T">The type of the asynchronous state.</typeparam>
/// <remarks>This type is intended for internal use. Use <see cref="T:Microsoft.Extensions.AsyncState.IAsyncContext`1" /> instead.</remarks>
[Experimental("EXTEXP0006", UrlFormat = "https://aka.ms/dotnet-extensions-warnings/{0}")]
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IAsyncLocalContext<T> : IAsyncContext<T> where T : class
{
}

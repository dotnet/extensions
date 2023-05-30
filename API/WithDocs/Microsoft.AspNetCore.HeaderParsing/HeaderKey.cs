// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Used to indicate which header to parse.
/// </summary>
/// <typeparam name="T">The type of the header value.</typeparam>
public sealed class HeaderKey<T> where T : notnull
{
    /// <summary>
    /// Gets the name of the header.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Returns a string representing this instance.
    /// </summary>
    /// <returns>
    /// The name of this instance.
    /// </returns>
    public override string ToString();
}

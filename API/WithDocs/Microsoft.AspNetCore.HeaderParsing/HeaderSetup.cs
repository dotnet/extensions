// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Stores all setup information for a header.
/// </summary>
/// <typeparam name="THeader">The type of the header value.</typeparam>
public class HeaderSetup<THeader> where THeader : notnull
{
    /// <summary>
    /// Gets the name of the header.
    /// </summary>
    public string HeaderName { get; }

    /// <summary>
    /// Gets the type of the parser to parse header values.
    /// </summary>
    /// <remarks>Not <see langword="null" /> when <see cref="P:Microsoft.AspNetCore.HeaderParsing.HeaderSetup`1.ParserInstance" /> is <see langword="null" /> and vice versa.</remarks>
    public Type? ParserType { get; }

    /// <summary>
    /// Gets the parser to parse header values.
    /// </summary>
    /// <remarks>Not <see langword="null" /> when <see cref="P:Microsoft.AspNetCore.HeaderParsing.HeaderSetup`1.ParserType" /> is <see langword="null" /> and vice versa.</remarks>
    public HeaderParser<THeader>? ParserInstance { get; }

    /// <summary>
    /// Gets a value indicating whether this header's parsed values can and/or should be cached.
    /// </summary>
    public bool Cacheable { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.AspNetCore.HeaderParsing.HeaderSetup`1" /> class.
    /// </summary>
    /// <param name="headerName">The name of the header.</param>
    /// <param name="parserType">The type of the parser to parse header values.</param>
    /// <param name="cacheable">Indicates whether the header's values can be cached.</param>
    public HeaderSetup(string headerName, Type parserType, bool cacheable = false);

    /// <summary>
    /// Initializes a new instance of the <see cref="T:Microsoft.AspNetCore.HeaderParsing.HeaderSetup`1" /> class.
    /// </summary>
    /// <param name="headerName">The name of the header.</param>
    /// <param name="instance">The parser to parse header values.</param>
    /// <param name="cacheable">Indicates whether the header's values can be cached.</param>
    public HeaderSetup(string headerName, HeaderParser<THeader> instance, bool cacheable = false);
}

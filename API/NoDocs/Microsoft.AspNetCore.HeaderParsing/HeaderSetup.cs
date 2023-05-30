// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.HeaderParsing;

public class HeaderSetup<THeader> where THeader : notnull
{
    public string HeaderName { get; }
    public Type? ParserType { get; }
    public HeaderParser<THeader>? ParserInstance { get; }
    public bool Cacheable { get; }
    public HeaderSetup(string headerName, Type parserType, bool cacheable = false);
    public HeaderSetup(string headerName, HeaderParser<THeader> instance, bool cacheable = false);
}

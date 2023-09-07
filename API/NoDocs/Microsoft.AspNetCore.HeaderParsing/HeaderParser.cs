// Assembly 'Microsoft.AspNetCore.HeaderParsing'

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderParsing;

public abstract class HeaderParser<T> where T : notnull
{
    public abstract bool TryParse(StringValues values, [NotNullWhen(true)] out T? result, [NotNullWhen(false)] out string? error);
    protected HeaderParser();
}

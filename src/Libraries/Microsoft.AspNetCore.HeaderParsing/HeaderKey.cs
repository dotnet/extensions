// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.HeaderParsing;

/// <summary>
/// Used to indicate which header to parse.
/// </summary>
/// <typeparam name="T">The type of the header value.</typeparam>
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Header keys are never disposed, so we don't bother with this.")]
[SuppressMessage("Blocker Bug", "S2931:Classes with \"IDisposable\" members should implement \"IDisposable\"", Justification = "Header keys are never disposed, so we don't bother with this.")]
public sealed class HeaderKey<T>
    where T : notnull
{
    /// <summary>
    /// Gets the name of the header.
    /// </summary>
    public string Name { get; }

    internal bool HasDefaultValue { get; }
    internal T? DefaultValue { get; }
    internal int Position { get; }
    internal HeaderParser<T> Parser { get; }
    private readonly IMemoryCache? _valueCache;

    internal HeaderKey(string name, HeaderParser<T> parser, int position, int maxCachedValues = 0)
    {
        Name = name;
        Position = position;
        Parser = parser;

        if (maxCachedValues > 0)
        {
            var o = new MemoryCacheOptions
            {
                SizeLimit = maxCachedValues,
            };

            _valueCache = new MemoryCache(Options.Create(o));
        }
    }

    internal HeaderKey(string name, HeaderParser<T> parser, int position, int maxCachedValues, T defaultValue)
        : this(name, parser, position, maxCachedValues)
    {
        DefaultValue = defaultValue;
        HasDefaultValue = true;
    }

    /// <summary>
    /// Returns a string representing this instance.
    /// </summary>
    /// <returns>
    /// The name of this instance.
    /// </returns>
    public override string ToString() => Name;

    private static readonly MemoryCacheEntryOptions _cacheEntryOptions = new()
    {
        Size = 1,
    };

    internal bool TryParse(StringValues values, out T? result, out string? error) => Parser.TryParse(values, out result, out error);
    internal void AddCachedValue(StringValues values, object o) => _valueCache!.Set(values, o, _cacheEntryOptions);
    internal object? GetCachedValue(StringValues values) => _valueCache!.Get(values);
    internal bool Cacheable => _valueCache != null;
}

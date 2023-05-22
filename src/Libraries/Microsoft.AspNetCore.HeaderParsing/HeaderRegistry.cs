// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Shared.Diagnostics;

namespace Microsoft.AspNetCore.HeaderParsing;

internal sealed class HeaderRegistry : IHeaderRegistry
{
    private readonly HeaderParsingOptions _options;
    private readonly IServiceProvider _provider;
    private readonly ConcurrentDictionary<HeaderKeyIdentity, object> _headerKeys = new();
    private int _current = -1;

    public HeaderRegistry(IServiceProvider provider, IOptions<HeaderParsingOptions> options)
    {
        _provider = provider;
        _options = options.Value;
    }

    public HeaderKey<T> Register<T>(HeaderSetup<T> setup)
        where T : notnull
    {
        var parser = setup.ParserInstance ?? (HeaderParser<T>)_provider.GetRequiredService(setup.ParserType!);
        var id = new HeaderKeyIdentity(setup.HeaderName, parser, setup.Cacheable);
        return (HeaderKey<T>)_headerKeys.GetOrAdd(id, CreateKey<T>, parser);
    }

    private HeaderKey<T> CreateKey<T>(HeaderKeyIdentity id, HeaderParser<T> parser)
        where T : notnull
    {
        int maxCachedValues = 0;
        if (id.Cacheable)
        {
            if (!_options.MaxCachedValuesPerHeader.TryGetValue(id.HeaderName, out maxCachedValues))
            {
                maxCachedValues = _options.DefaultMaxCachedValuesPerHeader;
            }
        }

        var pos = Interlocked.Increment(ref _current);
        if (_options.DefaultValues.TryGetValue(id.HeaderName, out var defValue))
        {
            if (!parser.TryParse(defValue, out var parsedValue, out var error))
            {
                Throw.InvalidOperationException($"Can't parse default value '{defValue}' for header '{id.HeaderName}': {error}.");
            }

            return new HeaderKey<T>(id.HeaderName, parser, pos, maxCachedValues, parsedValue);
        }

        return new HeaderKey<T>(id.HeaderName, parser, pos, maxCachedValues);
    }

    [ExcludeFromCodeCoverage]
    private readonly record struct HeaderKeyIdentity(
        string HeaderName,
        object Parser,
        bool Cacheable);
}

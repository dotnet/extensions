// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class SizeTests
{
    [Theory]
    [InlineData(null, true)] // does not enforce size limits
    [InlineData(8L, false)] // unreasonably small limt; chosen because our test string has length 12 - hence no expectation to find the second time
    [InlineData(1024L, true)] // reasonable size limit
    public async Task ValidateSizeLimit_Immutable(long? sizeLimit, bool expectFromL1)
    {
        var services = new ServiceCollection();
        services.AddMemoryCache(options => options.SizeLimit = sizeLimit);
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        const string Key = "abc";

        // this looks weird; it is intentionally not a const - we want to check
        // same instance without worrying about interning from raw literals
        string expected = new("simple value".ToArray());
        var actual = await cache.GetOrCreateAsync<string>(Key, ct => new(expected));

        // expect same contents
        Assert.Equal(expected, actual);

        // expect same instance, because string is special-cased as a type
        // that doesn't need defensive copies
        Assert.Same(expected, actual);

        // rinse and repeat, to check we get the value from L1
        actual = await cache.GetOrCreateAsync<string>(Key, ct => new(Guid.NewGuid().ToString()));

        if (expectFromL1)
        {
            // expect same contents from L1
            Assert.Equal(expected, actual);

            // expect same instance, because string is special-cased as a type
            // that doesn't need defensive copies
            Assert.Same(expected, actual);
        }
        else
        {
            // L1 cache not used
            Assert.NotEqual(expected, actual);
        }
    }

    [Theory]
    [InlineData(null, true)] // does not enforce size limits
    [InlineData(8L, false)] // unreasonably small limt; chosen because our test string has length 12 - hence no expectation to find the second time
    [InlineData(1024L, true)] // reasonable size limit
    public async Task ValidateSizeLimit_Mutable(long? sizeLimit, bool expectFromL1)
    {
        var services = new ServiceCollection();
        services.AddMemoryCache(options => options.SizeLimit = sizeLimit);
        services.AddHybridCache();
        using var provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        const string Key = "abc";

        string expected = "simple value";
        var actual = await cache.GetOrCreateAsync<MutablePoco>(Key, ct => new(new MutablePoco { Value = expected }));

        // expect same contents
        Assert.Equal(expected, actual.Value);

        // rinse and repeat, to check we get the value from L1
        actual = await cache.GetOrCreateAsync<MutablePoco>(Key, ct => new(new MutablePoco { Value = Guid.NewGuid().ToString() }));

        if (expectFromL1)
        {
            // expect same contents from L1
            Assert.Equal(expected, actual.Value);
        }
        else
        {
            // L1 cache not used
            Assert.NotEqual(expected, actual.Value);
        }
    }

    public class MutablePoco
    {
        public string Value { get; set; } = "";
    }
}

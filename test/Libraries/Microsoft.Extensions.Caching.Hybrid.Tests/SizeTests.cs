// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class SizeTests(ITestOutputHelper log)
{
    [Theory]
    [InlineData("abc", null, true, null, null)] // does not enforce size limits
    [InlineData("", null, false, null, null, Log.IdKeyEmptyOrWhitespace, Log.IdKeyEmptyOrWhitespace)] // invalid key
    [InlineData("  ", null, false, null, null, Log.IdKeyEmptyOrWhitespace, Log.IdKeyEmptyOrWhitespace)] // invalid key
    [InlineData(null, null, false, null, null, Log.IdKeyEmptyOrWhitespace, Log.IdKeyEmptyOrWhitespace)] // invalid key
    [InlineData("abc", 8L, false, null, null)] // unreasonably small limit; chosen because our test string has length 12 - hence no expectation to find the second time
    [InlineData("abc", 1024L, true, null, null)] // reasonable size limit
    [InlineData("abc", 1024L, true, 8L, null, Log.IdMaximumPayloadBytesExceeded)] // reasonable size limit, small HC quota
    [InlineData("abc", null, false, null, 2, Log.IdMaximumKeyLengthExceeded, Log.IdMaximumKeyLengthExceeded)] // key limit exceeded
    public async Task ValidateSizeLimit_Immutable(string key, long? sizeLimit, bool expectFromL1, long? maximumPayloadBytes, int? maximumKeyLength,
        params int[] errorIds)
    {
        using var collector = new LogCollector();
        var services = new ServiceCollection();
        services.AddMemoryCache(options => options.SizeLimit = sizeLimit);
        services.AddHybridCache(options =>
        {
            if (maximumKeyLength.HasValue)
            {
                options.MaximumKeyLength = maximumKeyLength.GetValueOrDefault();
            }

            if (maximumPayloadBytes.HasValue)
            {
                options.MaximumPayloadBytes = maximumPayloadBytes.GetValueOrDefault();
            }
        });
        services.AddLogging(options =>
        {
            options.ClearProviders();
            options.AddProvider(collector);
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        // this looks weird; it is intentionally not a const - we want to check
        // same instance without worrying about interning from raw literals
        string expected = new("simple value".ToArray());
        var actual = await cache.GetOrCreateAsync<string>(key, ct => new(expected));

        // expect same contents
        Assert.Equal(expected, actual);

        // expect same instance, because string is special-cased as a type
        // that doesn't need defensive copies
        Assert.Same(expected, actual);

        // rinse and repeat, to check we get the value from L1
        actual = await cache.GetOrCreateAsync<string>(key, ct => new(Guid.NewGuid().ToString()));

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

        collector.WriteTo(log);
        collector.AssertErrors(errorIds);
    }

    [Theory]
    [InlineData("abc", null, true, null, null)] // does not enforce size limits
    [InlineData("", null, false, null, null, Log.IdKeyEmptyOrWhitespace, Log.IdKeyEmptyOrWhitespace)] // invalid key
    [InlineData("  ", null, false, null, null, Log.IdKeyEmptyOrWhitespace, Log.IdKeyEmptyOrWhitespace)] // invalid key
    [InlineData(null, null, false, null, null, Log.IdKeyEmptyOrWhitespace, Log.IdKeyEmptyOrWhitespace)] // invalid key
    [InlineData("abc", 8L, false, null, null)] // unreasonably small limit; chosen because our test string has length 12 - hence no expectation to find the second time
    [InlineData("abc", 1024L, true, null, null)] // reasonable size limit
    [InlineData("abc", 1024L, true, 8L, null, Log.IdMaximumPayloadBytesExceeded)] // reasonable size limit, small HC quota
    [InlineData("abc", null, false, null, 2, Log.IdMaximumKeyLengthExceeded, Log.IdMaximumKeyLengthExceeded)] // key limit exceeded
    public async Task ValidateSizeLimit_Mutable(string key, long? sizeLimit, bool expectFromL1, long? maximumPayloadBytes, int? maximumKeyLength,
        params int[] errorIds)
    {
        using var collector = new LogCollector();
        var services = new ServiceCollection();
        services.AddMemoryCache(options => options.SizeLimit = sizeLimit);
        services.AddHybridCache(options =>
        {
            if (maximumKeyLength.HasValue)
            {
                options.MaximumKeyLength = maximumKeyLength.GetValueOrDefault();
            }

            if (maximumPayloadBytes.HasValue)
            {
                options.MaximumPayloadBytes = maximumPayloadBytes.GetValueOrDefault();
            }
        });
        services.AddLogging(options =>
        {
            options.ClearProviders();
            options.AddProvider(collector);
        });
        using ServiceProvider provider = services.BuildServiceProvider();
        var cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());

        string expected = "simple value";
        var actual = await cache.GetOrCreateAsync<MutablePoco>(key, ct => new(new MutablePoco { Value = expected }));

        // expect same contents
        Assert.Equal(expected, actual.Value);

        // rinse and repeat, to check we get the value from L1
        actual = await cache.GetOrCreateAsync<MutablePoco>(key, ct => new(new MutablePoco { Value = Guid.NewGuid().ToString() }));

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

        collector.WriteTo(log);
        collector.AssertErrors(errorIds);
    }

    public class MutablePoco
    {
        public string Value { get; set; } = "";
    }
}

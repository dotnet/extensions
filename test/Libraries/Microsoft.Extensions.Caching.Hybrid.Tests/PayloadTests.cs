// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;
using static Microsoft.Extensions.Caching.Hybrid.Tests.DistributedCacheTests;
using static Microsoft.Extensions.Caching.Hybrid.Tests.L2Tests;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;

public class PayloadTests(ITestOutputHelper log) : IClassFixture<TestEventListener>
{
    private static ServiceProvider GetDefaultCache(out DefaultHybridCache cache, Action<ServiceCollection>? config = null)
    {
        var services = new ServiceCollection();
        config?.Invoke(services);
        services.AddHybridCache();
        ServiceProvider provider = services.BuildServiceProvider();
        cache = Assert.IsType<DefaultHybridCache>(provider.GetRequiredService<HybridCache>());
        return provider;
    }

    [Theory]
    [InlineData("", 1054, 0)]
    [InlineData("some_tag", 1063, 1)]
    [InlineData("some_tag,another_tag", 1075, 2)]
    public void RoundTrip_Success(string delimitedTags, int expectedLength, int tagCount)
    {
        var clock = new FakeTime();
        using var provider = GetDefaultCache(out var cache, config =>
        {
            config.AddSingleton<TimeProvider>(clock);
        });

        byte[] bytes = new byte[1024];
        new Random().NextBytes(bytes);

        string key = "my key";
        var tags = string.IsNullOrEmpty(delimitedTags)
            ? TagSet.Empty : TagSet.Create(delimitedTags.Split(','));
        Assert.Equal(tagCount, tags.Count);

        var maxLen = HybridCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = HybridCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");

        Assert.Equal(expectedLength, actualLength);

        clock.Add(TimeSpan.FromSeconds(10));
        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var remaining, out var flags, out var entropy, out var pendingTags, out _, out _);
        log.WriteLine($"Entropy: {entropy}; Flags: {flags}");
        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.Success, result);
        Assert.True(payload.SequenceEqual(bytes));
        Assert.True(pendingTags.IsEmpty);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(42L)]
    [InlineData(long.MaxValue)]
    public void RoundTrip_LocalCacheSize(long? localCacheSize)
    {
        using var provider = GetDefaultCache(out var cache);

        byte[] bytes = new byte[64];
        new Random().NextBytes(bytes);

        string key = "k";
        TagSet tags = TagSet.Empty;
        int maxLen = HybridCachePayload.GetMaxBytes(key, tags, bytes.Length);
        byte[] oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = HybridCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes), localCacheSize: localCacheSize);

        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache,
            out var payload, out _, out var flags, out _, out var pendingTags, out long? parsedSize, out _);

        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.Success, result);
        Assert.True(payload.SequenceEqual(bytes));
        Assert.True(pendingTags.IsEmpty);
        Assert.Equal(localCacheSize, parsedSize);
        Assert.Equal(localCacheSize is not null, (flags & HybridCachePayload.PayloadFlags.HasLocalSize) != 0);

        ArrayPool<byte>.Shared.Return(oversized);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(-100L)]
    public void RoundTrip_LocalCacheSize_NegativeSentinelIsNormalizedToAbsent(long localCacheSize)
    {
        // negative LocalSize is the documented "reset to default" sentinel; Write must drop it
        // so the payload is emitted as v1 with no HasLocalSize flag.
        using var provider = GetDefaultCache(out var cache);

        byte[] bytes = new byte[8];
        string key = "k";
        TagSet tags = TagSet.Empty;
        int maxLen = HybridCachePayload.GetMaxBytes(key, tags, bytes.Length);
        byte[] oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = HybridCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes), localCacheSize: localCacheSize);

        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache,
            out _, out _, out var flags, out _, out _, out long? parsedSize, out _);

        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.Success, result);
        Assert.Null(parsedSize);
        Assert.Equal(HybridCachePayload.PayloadFlags.None, flags & HybridCachePayload.PayloadFlags.HasLocalSize);

        ArrayPool<byte>.Shared.Return(oversized);
    }

    [Fact]
    public void RoundTrip_SelfExpiration()
    {
        var clock = new FakeTime();
        using var provider = GetDefaultCache(out var cache, config =>
        {
            config.AddSingleton<TimeProvider>(clock);
        });

        byte[] bytes = new byte[1024];
        new Random().NextBytes(bytes);

        string key = "my key";
        var tags = TagSet.Create(["some_tag"]);
        var maxLen = HybridCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = HybridCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        clock.Add(TimeSpan.FromSeconds(58));
        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var remaining, out var flags, out var entropy, out var pendingTags, out _, out _);
        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.Success, result);
        Assert.True(payload.SequenceEqual(bytes));
        Assert.True(pendingTags.IsEmpty);

        clock.Add(TimeSpan.FromSeconds(4));
        result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out payload, out remaining, out flags, out entropy, out pendingTags, out _, out _);
        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.ExpiredByEntry, result);
        Assert.Equal(0, payload.Count);
        Assert.True(pendingTags.IsEmpty);
    }

    [Fact]
    public async Task RoundTrip_WildcardExpiration()
    {
        var clock = new FakeTime();
        using var provider = GetDefaultCache(out var cache, config =>
        {
            config.AddSingleton<TimeProvider>(clock);
        });

        byte[] bytes = new byte[1024];
        new Random().NextBytes(bytes);

        string key = "my key";
        var tags = TagSet.Create(["some_tag"]);
        var maxLen = HybridCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = HybridCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        clock.Add(TimeSpan.FromSeconds(2));
        await cache.RemoveByTagAsync("*");

        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var remaining, out var flags, out var entropy, out var pendingTags, out _, out _);
        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.ExpiredByWildcard, result);
        Assert.Equal(0, payload.Count);
        Assert.True(pendingTags.IsEmpty);
    }

    [Fact]
    public async Task RoundTrip_TagExpiration()
    {
        var clock = new FakeTime();
        using var provider = GetDefaultCache(out var cache, config =>
        {
            config.AddSingleton<TimeProvider>(clock);
        });

        byte[] bytes = new byte[1024];
        new Random().NextBytes(bytes);

        string key = "my key";
        var tags = TagSet.Create(["some_tag"]);
        var maxLen = HybridCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = HybridCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        clock.Add(TimeSpan.FromSeconds(2));
        await cache.RemoveByTagAsync("other_tag");

        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var remaining, out var flags, out var entropy, out var pendingTags, out _, out _);
        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.Success, result);
        Assert.True(payload.SequenceEqual(bytes));
        Assert.True(pendingTags.IsEmpty);

        await cache.RemoveByTagAsync("some_tag");
        result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out payload, out remaining, out flags, out entropy, out pendingTags, out _, out _);
        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.ExpiredByTag, result);
        Assert.Equal(0, payload.Count);
        Assert.True(pendingTags.IsEmpty);
    }

    [Fact]
    public async Task RoundTrip_TagExpiration_Pending()
    {
        var clock = new FakeTime();
        using var provider = GetDefaultCache(out var cache, config =>
        {
            config.AddSingleton<TimeProvider>(clock);
        });

        byte[] bytes = new byte[1024];
        new Random().NextBytes(bytes);

        string key = "my key";
        var tags = TagSet.Create(["some_tag"]);
        var maxLen = HybridCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        var creation = cache.CurrentTimestamp();
        int actualLength = HybridCachePayload.Write(oversized, key, creation, TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        clock.Add(TimeSpan.FromSeconds(2));

        var tcs = new TaskCompletionSource<long>();
        cache.DebugInvalidateTag("some_tag", tcs.Task);
        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var remaining, out var flags, out var entropy, out var pendingTags, out _, out _);
        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.Success, result);
        Assert.True(payload.SequenceEqual(bytes));
        Assert.Equal(1, pendingTags.Count);
        Assert.Equal("some_tag", pendingTags[0]);

        tcs.SetResult(cache.CurrentTimestamp());
        Assert.True(await cache.IsAnyTagExpiredAsync(pendingTags, creation));
    }

    [Fact]
    public void Gibberish()
    {
        var clock = new FakeTime();
        using var provider = GetDefaultCache(out var cache, config =>
        {
            config.AddSingleton<TimeProvider>(clock);
        });

        byte[] bytes = new byte[1024];
        new Random().NextBytes(bytes);

        var result = HybridCachePayload.TryParse(new(bytes), "whatever", TagSet.Empty, cache, out var payload, out var remaining, out var flags, out var entropy, out var pendingTags, out _, out _);
        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.FormatNotRecognized, result);
        Assert.Equal(0, payload.Count);
        Assert.True(pendingTags.IsEmpty);
    }

    [Fact]
    public void RoundTrip_Truncated()
    {
        var clock = new FakeTime();
        using var provider = GetDefaultCache(out var cache, config =>
        {
            config.AddSingleton<TimeProvider>(clock);
        });

        byte[] bytes = new byte[1024];
        new Random().NextBytes(bytes);

        string key = "my key";
        var tags = TagSet.Create(["some_tag"]);
        var maxLen = HybridCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = HybridCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength - 1), key, tags, cache, out var payload, out var remaining, out var flags, out var entropy, out var pendingTags, out _, out _);
        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.InvalidData, result);
        Assert.Equal(0, payload.Count);
        Assert.True(pendingTags.IsEmpty);
    }

    [Fact]
    public void RoundTrip_Oversized()
    {
        var clock = new FakeTime();
        using var provider = GetDefaultCache(out var cache, config =>
        {
            config.AddSingleton<TimeProvider>(clock);
        });

        byte[] bytes = new byte[1024];
        new Random().NextBytes(bytes);

        string key = "my key";
        var tags = TagSet.Create(["some_tag"]);
        var maxLen = HybridCachePayload.GetMaxBytes(key, tags, bytes.Length) + 1;
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = HybridCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength + 1), key, tags, cache, out var payload, out var remaining, out var flags, out var entropy, out var pendingTags, out _, out _);
        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.InvalidData, result);
        Assert.Equal(0, payload.Count);
        Assert.True(pendingTags.IsEmpty);
    }

    [Fact]
    public async Task MalformedKeyDetected()
    {
        using var collector = new LogCollector();
        using var provider = GetDefaultCache(out var cache, config =>
        {
            var localCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            config.AddSingleton<IDistributedCache>(new LoggingCache(log, localCache));
            config.AddLogging(options =>
            {
                options.ClearProviders();
                options.AddProvider(collector);
            });
        });

        byte[] bytes = new byte[1024];
        new Random().NextBytes(bytes);

        string key = "my\uD801\uD802key"; // malformed
        string[] tags = ["mytag"];

        _ = await cache.GetOrCreateAsync<Guid>(key, ct => new(Guid.NewGuid()), tags: tags);

        collector.WriteTo(log);
        collector.AssertErrors([Log.IdKeyInvalidUnicode]);
    }

    [Fact]
    public async Task MalformedTagDetected()
    {
        using var collector = new LogCollector();
        using var provider = GetDefaultCache(out var cache, config =>
        {
            var localCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            config.AddSingleton<IDistributedCache>(new LoggingCache(log, localCache));
            config.AddLogging(options =>
            {
                options.ClearProviders();
                options.AddProvider(collector);
            });
        });

        byte[] bytes = new byte[1024];
        new Random().NextBytes(bytes);

        string key = "my key"; // malformed
        string[] tags = ["my\uD801\uD802tag"];

        _ = await cache.GetOrCreateAsync<Guid>(key, ct => new(Guid.NewGuid()), tags: tags);

        collector.WriteTo(log);
        collector.AssertErrors([Log.IdTagInvalidUnicode]);
    }

    public enum FactoryMutation
    {
        MutateNonFlags,
        SetFlagsToNone,
        SetFlagsReadSideOnly,
        CallerDisabledL2Write_FactoryClears,
    }

    [Theory]
    [InlineData(FactoryMutation.MutateNonFlags)]
    [InlineData(FactoryMutation.SetFlagsToNone)]
    [InlineData(FactoryMutation.SetFlagsReadSideOnly)]
    [InlineData(FactoryMutation.CallerDisabledL2Write_FactoryClears)]
    public async Task MalformedKey_DoesNotWriteToL2_EvenWhenFactoryMutatesOptions(FactoryMutation mutation)
    {
        // When the key fails unicode validation, BackgroundFetchAsync makes sure that the (corrupted) key/tags are
        // not persisted to L2. ApplyFactoryOptions must preserve that safeguard.
        using var collector = new LogCollector();
        MemoryDistributedCache? localCache = null;
        using var provider = GetDefaultCache(out var cache, config =>
        {
            localCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            config.AddSingleton<IDistributedCache>(new LoggingCache(log, localCache));
            config.AddLogging(options =>
            {
                options.ClearProviders();
                options.AddProvider(collector);
            });
        });

        Assert.NotNull(localCache);

        string key = "my\uD801\uD802key"; // malformed unicode
        string[] tags = ["mytag"];

        HybridCacheEntryFlags callerFlags = mutation == FactoryMutation.CallerDisabledL2Write_FactoryClears
            ? HybridCacheEntryFlags.DisableDistributedCacheWrite
            : HybridCacheEntryFlags.None;

        _ = await cache.GetOrCreateAsync(
            key,
            (entryOptions, _) =>
            {
                switch (mutation)
                {
                    case FactoryMutation.MutateNonFlags:
                        entryOptions.LocalSize = 1234;
                        break;
                    case FactoryMutation.SetFlagsToNone:
                        entryOptions.Flags = HybridCacheEntryFlags.None;
                        break;
                    case FactoryMutation.SetFlagsReadSideOnly:
                        entryOptions.Flags = HybridCacheEntryFlags.DisableLocalCacheRead;
                        break;
                    case FactoryMutation.CallerDisabledL2Write_FactoryClears:
                        entryOptions.Flags = HybridCacheEntryFlags.None;
                        break;
                }

                return new ValueTask<Guid>(Guid.NewGuid());
            },
            options: new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(1), Flags = callerFlags },
            tags: tags);

        // Wait until the unicode-validation log fires (synchronous, inside BackgroundFetchAsync,
        // just before the L2 write decision); then give the background work a brief moment
        // to flush any erroneous L2 SetAsync, mirroring the pattern in RedisTests.
        await collector.WaitForLogsAsync([Log.IdKeyInvalidUnicode], TimeSpan.FromSeconds(5));
        await Task.Delay(500);

        collector.WriteTo(log);
        collector.AssertErrors([Log.IdKeyInvalidUnicode]);

        // The corrupted key must NOT have been persisted to L2. (Note: unrelated
        // tag-invalidation reads against valid tag keys may still appear in the backend.)
        Assert.Null(localCache.Get(key));
    }

    [Fact]
    public async Task FactoryCanReEnableL2Write_ThatCallerDisabled()
    {
        // Positive counterpart to the malformed-key tests: when the key is valid and the
        // caller passed DisableDistributedCacheWrite, the factory is allowed to clear that
        // bit on its options and the value should then be persisted to L2.
        MemoryDistributedCache? localCache = null;
        using var provider = GetDefaultCache(out var cache, config =>
        {
            localCache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
            config.AddSingleton<IDistributedCache>(new LoggingCache(log, localCache));
        });

        Assert.NotNull(localCache);

        string key = "my-valid-key";

        _ = await cache.GetOrCreateAsync(
            key,
            (entryOptions, _) =>
            {
                entryOptions.Flags = HybridCacheEntryFlags.None;
                return new ValueTask<Guid>(Guid.NewGuid());
            },
            options: new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(1),
                Flags = HybridCacheEntryFlags.DisableDistributedCacheWrite,
            });

        // Give the background L2 write a chance to complete.
        await Task.Delay(500);

        Assert.NotNull(localCache.Get(key));
    }

    [Theory]
    [InlineData("tag1,tag2", 2)]
    [InlineData("tag1,tag2,tag3", 3)]
    public void RoundTrip_WithPendingTags_WhenKnownTagsMismatch(string delimitedTags, int tagCount)
    {
        var clock = new FakeTime();
        using var provider = GetDefaultCache(out var cache, config =>
        {
            config.AddSingleton<TimeProvider>(clock);
        });

        byte[] bytes = new byte[1024];
        new Random().NextBytes(bytes);

        string key = "my key";
        string[] tagsArray = delimitedTags.Split(',');
        var writeTags = TagSet.Create(tagsArray);
        Assert.Equal(tagCount, writeTags.Count);

        var maxLen = HybridCachePayload.GetMaxBytes(key, writeTags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = HybridCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, writeTags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");

        clock.Add(TimeSpan.FromSeconds(10));

        // Inject non-completed tasks for each tag so IsTagExpired returns isPending=true
        foreach (string tag in tagsArray)
        {
            cache.DebugInvalidateTag(tag, new TaskCompletionSource<long>().Task);
        }

        // Parse with empty knownTags to force all tags into pendingTags via the rented buffer path
        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, TagSet.Empty, cache,
            out var payload, out var remaining, out var flags, out var entropy, out var pendingTags, out _, out _);
        Assert.Equal(HybridCachePayload.HybridCachePayloadParseResult.Success, result);
        Assert.True(payload.SequenceEqual(bytes));
        Assert.Equal(tagCount, pendingTags.Count);
    }
}

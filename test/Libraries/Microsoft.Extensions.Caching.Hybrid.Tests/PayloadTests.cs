// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit.Abstractions;
using static Microsoft.Extensions.Caching.Hybrid.Tests.DistributedCacheTests;
using static Microsoft.Extensions.Caching.Hybrid.Tests.L2Tests;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class PayloadTests(ITestOutputHelper log)
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

    [Fact]
    public void RoundTrip_Success()
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

        clock.Add(TimeSpan.FromSeconds(10));
        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        log.WriteLine($"Entropy: {entropy}; Flags: {flags}");
        Assert.Equal(HybridCachePayload.ParseResult.Success, result);
        Assert.True(payload.SequenceEqual(bytes));
        Assert.True(pendingTags.IsEmpty);
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
        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(HybridCachePayload.ParseResult.Success, result);
        Assert.True(payload.SequenceEqual(bytes));
        Assert.True(pendingTags.IsEmpty);

        clock.Add(TimeSpan.FromSeconds(4));
        result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out payload, out flags, out entropy, out pendingTags);
        Assert.Equal(HybridCachePayload.ParseResult.ExpiredSelf, result);
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

        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(HybridCachePayload.ParseResult.ExpiredWildcard, result);
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

        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(HybridCachePayload.ParseResult.Success, result);
        Assert.True(payload.SequenceEqual(bytes));
        Assert.True(pendingTags.IsEmpty);

        await cache.RemoveByTagAsync("some_tag");
        result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out payload, out flags, out entropy, out pendingTags);
        Assert.Equal(HybridCachePayload.ParseResult.ExpiredTag, result);
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
        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(HybridCachePayload.ParseResult.Success, result);
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

        var result = HybridCachePayload.TryParse(new(bytes), "whatever", TagSet.Empty, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(HybridCachePayload.ParseResult.NotRecognized, result);
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

        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength - 1), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(HybridCachePayload.ParseResult.InvalidData, result);
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

        var result = HybridCachePayload.TryParse(new(oversized, 0, actualLength + 1), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(HybridCachePayload.ParseResult.InvalidData, result);
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
}

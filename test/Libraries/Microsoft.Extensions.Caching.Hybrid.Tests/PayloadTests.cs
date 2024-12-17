// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.Extensions.Caching.Hybrid.Internal;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using static Microsoft.Extensions.Caching.Hybrid.Tests.DistributedCacheTests;

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
        var maxLen = DistributedCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = DistributedCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        clock.Add(TimeSpan.FromSeconds(10));
        var result = DistributedCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        log.WriteLine($"Entropy: {entropy}; Flags: {flags}");
        Assert.Equal(DistributedCachePayload.ParseResult.Success, result);
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
        var maxLen = DistributedCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = DistributedCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        clock.Add(TimeSpan.FromSeconds(58));
        var result = DistributedCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(DistributedCachePayload.ParseResult.Success, result);
        Assert.True(payload.SequenceEqual(bytes));
        Assert.True(pendingTags.IsEmpty);

        clock.Add(TimeSpan.FromSeconds(4));
        result = DistributedCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out payload, out flags, out entropy, out pendingTags);
        Assert.Equal(DistributedCachePayload.ParseResult.ExpiredSelf, result);
        Assert.Equal(0, payload.Length);
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
        var maxLen = DistributedCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = DistributedCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        clock.Add(TimeSpan.FromSeconds(2));
        await cache.RemoveByTagAsync("*");

        var result = DistributedCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(DistributedCachePayload.ParseResult.ExpiredWildcard, result);
        Assert.Equal(0, payload.Length);
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
        var maxLen = DistributedCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = DistributedCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        clock.Add(TimeSpan.FromSeconds(2));
        await cache.RemoveByTagAsync("other_tag");

        var result = DistributedCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(DistributedCachePayload.ParseResult.Success, result);
        Assert.True(payload.SequenceEqual(bytes));
        Assert.True(pendingTags.IsEmpty);

        await cache.RemoveByTagAsync("some_tag");
        result = DistributedCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out payload, out flags, out entropy, out pendingTags);
        Assert.Equal(DistributedCachePayload.ParseResult.ExpiredTag, result);
        Assert.Equal(0, payload.Length);
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
        var maxLen = DistributedCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        var creation = cache.CurrentTimestamp();
        int actualLength = DistributedCachePayload.Write(oversized, key, creation, TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        clock.Add(TimeSpan.FromSeconds(2));

        var tcs = new TaskCompletionSource<long>();
        cache.DebugInvalidateTag("some_tag", tcs.Task);
        var result = DistributedCachePayload.TryParse(new(oversized, 0, actualLength), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(DistributedCachePayload.ParseResult.Success, result);
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

        var result = DistributedCachePayload.TryParse(bytes, "whatever", TagSet.Empty, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(DistributedCachePayload.ParseResult.NotRecognized, result);
        Assert.Equal(0, payload.Length);
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
        var maxLen = DistributedCachePayload.GetMaxBytes(key, tags, bytes.Length);
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = DistributedCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        var result = DistributedCachePayload.TryParse(new(oversized, 0, actualLength - 1), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(DistributedCachePayload.ParseResult.InvalidData, result);
        Assert.Equal(0, payload.Length);
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
        var maxLen = DistributedCachePayload.GetMaxBytes(key, tags, bytes.Length) + 1;
        var oversized = ArrayPool<byte>.Shared.Rent(maxLen);

        int actualLength = DistributedCachePayload.Write(oversized, key, cache.CurrentTimestamp(), TimeSpan.FromMinutes(1), 0, tags, new(bytes));
        log.WriteLine($"bytes written: {actualLength}");
        Assert.Equal(1063, actualLength);

        var result = DistributedCachePayload.TryParse(new(oversized, 0, actualLength + 1), key, tags, cache, out var payload, out var flags, out var entropy, out var pendingTags);
        Assert.Equal(DistributedCachePayload.ParseResult.InvalidData, result);
        Assert.Equal(0, payload.Length);
        Assert.True(pendingTags.IsEmpty);
    }
}

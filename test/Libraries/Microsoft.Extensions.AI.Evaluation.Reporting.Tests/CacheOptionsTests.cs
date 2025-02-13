// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;
using Xunit;
using CacheMode = Microsoft.Extensions.AI.Evaluation.Reporting.Storage.DiskBasedResponseCache.CacheMode;
using CacheOptions = Microsoft.Extensions.AI.Evaluation.Reporting.Storage.DiskBasedResponseCache.CacheOptions;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

public class CacheOptionsTests
{
    [Fact]
    public void SerializeCacheOptions()
    {
        var options = new CacheOptions(CacheMode.Disabled, TimeSpan.FromDays(300));

        string json = JsonSerializer.Serialize(options, SerializerContext.Default.CacheOptions);
        CacheOptions? deserialized = JsonSerializer.Deserialize<CacheOptions>(json, SerializerContext.Default.CacheOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(options.Mode, deserialized!.Mode);
        Assert.Equal(options.TimeToLiveForCacheEntries, deserialized.TimeToLiveForCacheEntries);

    }

    [Fact]
    public void SerializeCacheOptionsCompact()
    {
        var options = new CacheOptions(CacheMode.Disabled, TimeSpan.FromDays(300));

        string json = JsonSerializer.Serialize(options, SerializerContext.Compact.CacheOptions);
        CacheOptions? deserialized = JsonSerializer.Deserialize<CacheOptions>(json, SerializerContext.Default.CacheOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(options.Mode, deserialized!.Mode);
        Assert.Equal(options.TimeToLiveForCacheEntries, deserialized.TimeToLiveForCacheEntries);

    }

    [Fact]
    public void SerializeCacheOptionsToFile()
    {
        var options = new CacheOptions(CacheMode.Enabled, TimeSpan.FromSeconds(10));

        string tempFilePath = Path.GetTempFileName();
        options.Write(tempFilePath);
        CacheOptions deserialized = CacheOptions.Read(tempFilePath);

        Assert.NotNull(deserialized);
        Assert.Equal(options.Mode, deserialized.Mode);
        Assert.Equal(options.TimeToLiveForCacheEntries, deserialized.TimeToLiveForCacheEntries);
    }

    [Fact]
    public async Task SerializeCacheOptionsToFileAsync()
    {
        var options = new CacheOptions(CacheMode.Enabled, TimeSpan.FromSeconds(10));

        string tempFilePath = Path.GetTempFileName();
        await options.WriteAsync(tempFilePath);
        CacheOptions deserialized = await CacheOptions.ReadAsync(tempFilePath);

        Assert.NotNull(deserialized);
        Assert.Equal(options.Mode, deserialized.Mode);
        Assert.Equal(options.TimeToLiveForCacheEntries, deserialized.TimeToLiveForCacheEntries);
    }

}

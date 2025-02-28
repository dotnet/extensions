// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.AI.Evaluation.Reporting.JsonSerialization;
using Xunit;
using CacheEntry = Microsoft.Extensions.AI.Evaluation.Reporting.Storage.DiskBasedResponseCache.CacheEntry;

namespace Microsoft.Extensions.AI.Evaluation.Reporting.Tests;

public class CacheEntryTests
{
    [Fact]
    public void SerializeCacheEntry()
    {
        var entry =
            new CacheEntry(
                scenarioName: "Scenario1",
                iterationName: "Iteration2",
                creation: DateTime.UtcNow,
                expiration: DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)));

        string json = JsonSerializer.Serialize(entry, SerializerContext.Default.CacheEntry);
        CacheEntry? deserialized = JsonSerializer.Deserialize<CacheEntry>(json, SerializerContext.Default.CacheEntry);

        Assert.NotNull(deserialized);
        Assert.Equal(entry.ScenarioName, deserialized!.ScenarioName);
        Assert.Equal(entry.IterationName, deserialized.IterationName);
        Assert.Equal(entry.Creation, deserialized.Creation);
        Assert.Equal(entry.Expiration, deserialized.Expiration);
    }

    [Fact]
    public void SerializeCacheEntryCompact()
    {
        var entry =
            new CacheEntry(
                scenarioName: "Scenario1",
                iterationName: "Iteration2",
                creation: DateTime.UtcNow,
                expiration: DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)));

        string json = JsonSerializer.Serialize(entry, SerializerContext.Compact.CacheEntry);
        CacheEntry? deserialized = JsonSerializer.Deserialize<CacheEntry>(json, SerializerContext.Default.CacheEntry);

        Assert.NotNull(deserialized);
        Assert.Equal(entry.ScenarioName, deserialized!.ScenarioName);
        Assert.Equal(entry.IterationName, deserialized.IterationName);
        Assert.Equal(entry.Creation, deserialized.Creation);
        Assert.Equal(entry.Expiration, deserialized.Expiration);
    }

    [Fact]
    public void SerializeCacheEntryToFile()
    {
        var entry =
            new CacheEntry(
                scenarioName: "Scenario1",
                iterationName: "Iteration2",
                creation: DateTime.UtcNow,
                expiration: DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)));

        string tempFilePath = Path.GetTempFileName();
        entry.Write(tempFilePath);
        CacheEntry? deserialized = CacheEntry.Read(tempFilePath);

        Assert.NotNull(deserialized);
        Assert.Equal(entry.ScenarioName, deserialized.ScenarioName);
        Assert.Equal(entry.IterationName, deserialized.IterationName);
        Assert.Equal(entry.Creation, deserialized.Creation);
        Assert.Equal(entry.Expiration, deserialized.Expiration);
    }

    [Fact]
    public async Task SerializeCacheEntryToFileAsync()
    {
        var entry =
            new CacheEntry(
                scenarioName: "Scenario1",
                iterationName: "Iteration2",
                creation: DateTime.UtcNow,
                expiration: DateTime.UtcNow.Add(TimeSpan.FromMinutes(5)));

        string tempFilePath = Path.GetTempFileName();
        await entry.WriteAsync(tempFilePath);
        CacheEntry? deserialized = await CacheEntry.ReadAsync(tempFilePath);

        Assert.NotNull(deserialized);
        Assert.Equal(entry.ScenarioName, deserialized.ScenarioName);
        Assert.Equal(entry.IterationName, deserialized.IterationName);
        Assert.Equal(entry.Creation, deserialized.Creation);
        Assert.Equal(entry.Expiration, deserialized.Expiration);
    }

}

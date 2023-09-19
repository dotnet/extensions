// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Internal.Test;

public class MetricEnrichmentTagCollectorTest
{
    [Fact]
    public void AddProperty_AllVersions_WithNullKey_Throws()
    {
        var collector = new MetricEnrichmentTagCollector();
        Assert.Throws<ArgumentNullException>(() => collector.Add(null!, 1));
        Assert.Throws<ArgumentNullException>(() => collector.Add(null!, "testString"));
    }

    [Fact]
    public void AddProperty_AllVersions_WithEmptyKey_Throws()
    {
        var collector = new MetricEnrichmentTagCollector();
        Assert.Throws<ArgumentException>(() => collector.Add("", 1));
        Assert.Throws<ArgumentException>(() => collector.Add(string.Empty, "testString"));
    }

    [Fact]
    public void AddProperty_AllVersions_WithNullValue_Throws()
    {
        var collector = new MetricEnrichmentTagCollector();
        Assert.Throws<ArgumentNullException>(() => collector.Add("key1", null!));
    }

    [Fact]
    public void AddProperty_EmptyString_DoesNotReplaceWithNull()
    {
        var collector = new MetricEnrichmentTagCollector
        {
            { "key1", "" }
        };

        Assert.Equal("key1", collector[0].Key);
        Assert.Equal("", collector[0].Value);
    }

    [Fact]
    public void AddProperty_ValidKeyAndValues_AddedCorrectly()
    {
        var collector = new MetricEnrichmentTagCollector
        {
            { "key1", 10 },
            { "key2", "value2" },
            { "key3", new NoString() }
        };

        Assert.Equal("key1", collector[0].Key);
        Assert.Equal("10", collector[0].Value);

        Assert.Equal("key2", collector[1].Key);
        Assert.Equal("value2", collector[1].Value);

        Assert.Equal("key3", collector[2].Key);
        Assert.Equal(string.Empty, collector[2].Value);
    }

    [Fact]
    public void AddProperty_ObjectSpan_AddedCorrectly()
    {
        var collector = new MetricEnrichmentTagCollector();

#pragma warning disable S3257 // Declarations and initializations should be as concise as possible
        var props = new KeyValuePair<string, object>[]
        {
            new("key1", 10),
            new("key2", "value2"),
            new("key3", new NoString()),
        };
#pragma warning restore S3257 // Declarations and initializations should be as concise as possible

        foreach (var prop in props)
        {
            collector.Add(prop.Key, prop.Value);
        }

        Assert.Equal("key1", collector[0].Key);
        Assert.Equal("10", collector[0].Value);

        Assert.Equal("key2", collector[1].Key);
        Assert.Equal("value2", collector[1].Value);

        Assert.Equal("key3", collector[2].Key);
        Assert.Equal(string.Empty, collector[2].Value);
    }

    [Fact]
    public void AddProperty_StringSpan_AddedCorrectly()
    {
        var collector = new MetricEnrichmentTagCollector();

#pragma warning disable S3257 // Declarations and initializations should be as concise as possible
        var props = new KeyValuePair<string, string>[]
        {
            new("key1", "10"),
            new("key2", "value2"),
        };
#pragma warning restore S3257 // Declarations and initializations should be as concise as possible

        foreach (var prop in props)
        {
            collector.Add(prop.Key, prop.Value);
        }

        Assert.Equal("key1", collector[0].Key);
        Assert.Equal("10", collector[0].Value);

        Assert.Equal("key2", collector[1].Key);
        Assert.Equal("value2", collector[1].Value);
    }

    [Fact]
    public void MetricEnrichmentPropertyBag_Reset_Clears()
    {
        var collector = new MetricEnrichmentTagCollector();

#pragma warning disable S3257 // Declarations and initializations should be as concise as possible
        var props = new KeyValuePair<string, string>[]
        {
            new("key1", "10"),
            new("key2", "value2"),
        };
#pragma warning restore S3257 // Declarations and initializations should be as concise as possible

        foreach (var prop in props)
        {
            collector.Add(prop.Key, prop.Value);
        }

        Assert.Equal("key1", collector[0].Key);
        Assert.Equal("10", collector[0].Value);

        Assert.Equal("key2", collector[1].Key);
        Assert.Equal("value2", collector[1].Value);

        _ = collector.TryReset();

        Assert.Empty(collector);
    }

    private class NoString
    {
        public override string? ToString() => null;
    }
}

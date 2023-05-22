// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Internal.Test;

public class MetricEnrichmentPropertyBagTest
{
    [Fact]
    public void AddProperty_AllVersions_WithNullKey_Throws()
    {
        var enrichmentBag = new MetricEnrichmentPropertyBag();
        Assert.Throws<ArgumentNullException>(() => enrichmentBag.Add(null!, 1));
        Assert.Throws<ArgumentNullException>(() => enrichmentBag.Add(null!, "testString"));
    }

    [Fact]
    public void AddProperty_AllVersions_WithEmptyKey_Throws()
    {
        var enrichmentBag = new MetricEnrichmentPropertyBag();
        Assert.Throws<ArgumentException>(() => enrichmentBag.Add("", 1));
        Assert.Throws<ArgumentException>(() => enrichmentBag.Add(string.Empty, "testString"));
    }

    [Fact]
    public void AddProperty_AllVersions_WithNullValue_Throws()
    {
        var enrichmentBag = new MetricEnrichmentPropertyBag();
        Assert.Throws<ArgumentNullException>(() => enrichmentBag.Add("key1", null!));
    }

    [Fact]
    public void AddProperty_EmptyString_DoesNotReplaceWithNull()
    {
        var enrichmentBag = new MetricEnrichmentPropertyBag
        {
            { "key1", "" }
        };

        Assert.Equal("key1", enrichmentBag[0].Key);
        Assert.Equal("", enrichmentBag[0].Value);
    }

    [Fact]
    public void AddProperty_ValidKeyAndValues_AddedCorrectly()
    {
        var enrichmentBag = new MetricEnrichmentPropertyBag
        {
            { "key1", 10 },
            { "key2", "value2" },
            { "key3", new NoString() }
        };

        Assert.Equal("key1", enrichmentBag[0].Key);
        Assert.Equal("10", enrichmentBag[0].Value);

        Assert.Equal("key2", enrichmentBag[1].Key);
        Assert.Equal("value2", enrichmentBag[1].Value);

        Assert.Equal("key3", enrichmentBag[2].Key);
        Assert.Equal(string.Empty, enrichmentBag[2].Value);
    }

    [Fact]
    public void AddProperty_ObjectSpan_AddedCorrectly()
    {
        var enrichmentBag = new MetricEnrichmentPropertyBag();

#pragma warning disable S3257 // Declarations and initializations should be as concise as possible
        var props = new KeyValuePair<string, object>[]
        {
            new("key1", 10),
            new("key2", "value2"),
            new("key3", new NoString()),
        };
#pragma warning restore S3257 // Declarations and initializations should be as concise as possible

        enrichmentBag.Add(props);

        Assert.Equal("key1", enrichmentBag[0].Key);
        Assert.Equal("10", enrichmentBag[0].Value);

        Assert.Equal("key2", enrichmentBag[1].Key);
        Assert.Equal("value2", enrichmentBag[1].Value);

        Assert.Equal("key3", enrichmentBag[2].Key);
        Assert.Equal(string.Empty, enrichmentBag[2].Value);
    }

    [Fact]
    public void AddProperty_StringSpan_AddedCorrectly()
    {
        var enrichmentBag = new MetricEnrichmentPropertyBag();

#pragma warning disable S3257 // Declarations and initializations should be as concise as possible
        var props = new KeyValuePair<string, string>[]
        {
            new("key1", "10"),
            new("key2", "value2"),
        };
#pragma warning restore S3257 // Declarations and initializations should be as concise as possible

        enrichmentBag.Add(props);

        Assert.Equal("key1", enrichmentBag[0].Key);
        Assert.Equal("10", enrichmentBag[0].Value);

        Assert.Equal("key2", enrichmentBag[1].Key);
        Assert.Equal("value2", enrichmentBag[1].Value);
    }

    [Fact]
    public void MetricEnrichmentPropertyBag_Reset_Clears()
    {
        var enrichmentBag = new MetricEnrichmentPropertyBag();

#pragma warning disable S3257 // Declarations and initializations should be as concise as possible
        var props = new KeyValuePair<string, string>[]
        {
            new("key1", "10"),
            new("key2", "value2"),
        };
#pragma warning restore S3257 // Declarations and initializations should be as concise as possible

        enrichmentBag.Add(props);

        Assert.Equal("key1", enrichmentBag[0].Key);
        Assert.Equal("10", enrichmentBag[0].Value);

        Assert.Equal("key2", enrichmentBag[1].Key);
        Assert.Equal("value2", enrichmentBag[1].Value);

        _ = enrichmentBag.TryReset();

        Assert.Empty(enrichmentBag);
    }

    private class NoString
    {
        public override string? ToString() => null;
    }
}

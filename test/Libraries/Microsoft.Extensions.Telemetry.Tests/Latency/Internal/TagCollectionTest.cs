// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Telemetry.Latency;
using Microsoft.Extensions.Telemetry.Latency.Internal;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Latency.Test.Internal;

public class TagCollectionTest
{
    private static readonly Registry _tagNames = new(new[] { "a", "b", "c", "d" }, false);

    [Fact]
    public void TagCollection_AddUnregisteredName()
    {
        TagCollection tc = new TagCollection(_tagNames);
        tc.Set(tc.GetToken("e"), "val");
        Assert.True(tc.Tags.Count == 4);
        tc.Tags.ToList().ForEach(t => Assert.True(t.Name != "e"));
    }

    [Fact]
    public void TagCollection_SetRegisteredNames()
    {
        TagCollection tc = new TagCollection(_tagNames);
        Dictionary<string, int> namesNumTimes = new Dictionary<string, int>
            {
                { "a", 0 },
                { "b", 0 },
                { "c", 0 }
            };

        foreach (var s in namesNumTimes.Keys)
        {
            tc.Set(tc.GetToken(s), "testVal");
        }

        var t = tc.Tags.ToList();
        Assert.True(t.Count == _tagNames.KeyCount);

        for (int i = 0; i < t.Count; i++)
        {
            var tagName = t[i].Name;
            Assert.True(_tagNames.GetRegisteredKeyIndex(tagName) > -1);
            if (namesNumTimes.ContainsKey(tagName))
            {
                // If tag was set, there should be be only instance of it.
                Assert.True(namesNumTimes[tagName] == 0);
                Assert.True(t[i].Value == "testVal");
                namesNumTimes[tagName]++;
            }
            else
            {
                // If tag was not set, value should be empty string.
                Assert.True(t[i].Value == string.Empty);
            }
        }
    }

    [Fact]
    public void TagCollection_Set_LastSetSetWins()
    {
        TagCollection tc = new TagCollection(_tagNames);
        tc.Set(tc.GetToken("a"), "first");
        tc.Set(tc.GetToken("a"), "second");
        Assert.True(tc.Tags.Count == _tagNames.KeyCount);
        var tagList = tc.Tags.ToList();

        // Verify only tag matches and has the last value set on it
        var atag = tagList.Where(t => t.Name == "a").ToList();
        Assert.True(atag.Count == 1);
        Assert.True(atag[0].Name == "a");
        Assert.True(atag[0].Value == "second");

        // All other tags must have empty string values.
        var notaTag = tagList.Where(t => t.Name != "a").ToList();
        Assert.True(notaTag.Count == tagList.Count - 1);
        var notaTagEmptyValues = notaTag.Where(t => t.Value == string.Empty).ToList();
        Assert.True(notaTagEmptyValues.Count == notaTag.Count);
    }

    [Fact]
    public void TagCollection_CheckReset()
    {
        TagCollection tc = new TagCollection(_tagNames);
        tc.Set(tc.GetToken("a"), "first");
        tc.Set(tc.GetToken("b"), "second");

        int numNonEmpty = GetNumberOfNonEmptyTags(tc.Tags);
        Assert.True(numNonEmpty == 2);

        _ = tc.TryReset();
        numNonEmpty = GetNumberOfNonEmptyTags(tc.Tags);
        Assert.True(numNonEmpty == 0);
    }

    private static int GetNumberOfNonEmptyTags(ReadOnlySpan<Tag> tags)
    {
        int numNonEmpty = 0;
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i].Value != string.Empty)
            {
                numNonEmpty++;
            }
        }

        return numNonEmpty;
    }
}

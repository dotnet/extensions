﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Compliance.Classification;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Diagnostics.Enrichment;
using Xunit;

namespace Microsoft.Extensions.Logging.Test;

public static class LoggerMessageStateTests
{
    [Fact]
    public static void Basic()
    {
        const string PropName = "Property Name";
        const string Value = "Value";

        var lms = new LoggerMessageState();

        var index = lms.ReserveTagSpace(1);
        lms.TagArray[index] = new(PropName, Value);
        Assert.Equal(1, lms.TagsCount);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);
        Assert.Equal("Property Name=Value", lms.ToString());

        lms.Clear();
        Assert.Equal(0, lms.TagsCount);
        Assert.Equal("", lms.ToString());

        index = lms.ReserveTagSpace(1);
        lms.TagArray[index] = new(PropName, Value);
        Assert.Equal(1, lms.TagsCount);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);
        Assert.Equal("Property Name=Value", lms.ToString());

        var set = new HashSet<DataClassification> { FakeClassifications.PrivateData };

        index = lms.ReserveClassifiedTagSpace(1);
        lms.ClassifiedTagArray[index] = new(PropName, Value, set.ToFrozenSet());
        Assert.Equal(1, lms.TagsCount);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);
        Assert.Equal("Property Name=Value,Property Name=System.Collections.Frozen.SmallValueTypeDefaultComparerFrozenSet`1[Microsoft.Extensions.Compliance.Classification.DataClassification]",
            lms.ToString());

        Assert.Equal(1, lms.ClassifiedTagsCount);
        Assert.Equal(PropName, lms.ClassifiedTagArray[0].Name);
        Assert.Equal(Value, lms.ClassifiedTagArray[0].Value);
        Assert.Equal(set.ToFrozenSet(), lms.ClassifiedTagArray[0].ClassificationsSet);
        Assert.Equal("Property Name=Value,Property Name=System.Collections.Frozen.SmallValueTypeDefaultComparerFrozenSet`1[Microsoft.Extensions.Compliance.Classification.DataClassification]",
            lms.ToString());

        index = lms.ReserveTagSpace(1);
        lms.TagArray[index] = new(PropName + "X", Value);
        Assert.Equal(2, lms.TagsCount);
        Assert.Equal(2, lms.TagArray.Length);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);
        Assert.Equal(PropName + "X", lms.TagArray[1].Key);
        Assert.Equal(Value, lms.TagArray[1].Value);
        Assert.Equal("Property Name=Value,Property NameX=Value,Property Name=System.Collections.Frozen.SmallValueTypeDefaultComparerFrozenSet`1" +
            "[Microsoft.Extensions.Compliance.Classification.DataClassification]", lms.ToString());
    }

    [Fact]
    public static void PropertyBagContract()
    {
        var lms = new LoggerMessageState();

        var collector = (IEnrichmentTagCollector)lms;

        collector.Add("K1", "V1");

        Assert.Equal(1, lms.TagsCount);
        Assert.Equal(0, lms.ClassifiedTagsCount);

        Assert.Equal("K1", lms.TagArray[0].Key);
        Assert.Equal("V1", lms.TagArray[0].Value);
    }

    [Fact]
    public static void CollectorContract()
    {
        const string PropertyNamPrefix = "param_name";
        const string PropName = "Property Name";
        const string Value = "Value";

        var lms = new LoggerMessageState();

        var collector = (ITagCollector)lms;
        lms.TagNamePrefix = PropertyNamPrefix;

        collector.Add(PropName, Value);
        Assert.Equal(1, lms.TagsCount);
        Assert.Equal(PropertyNamPrefix + "_" + PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);

        var set = new HashSet<DataClassification> { FakeClassifications.PrivateData };

        collector.Add(PropName, Value, set.ToFrozenSet());
        Assert.Equal(1, lms.TagsCount);
        Assert.Equal(PropertyNamPrefix + "_" + PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);

        Assert.Equal(1, lms.ClassifiedTagsCount);
        Assert.Equal(PropertyNamPrefix + "_" + PropName, lms.ClassifiedTagArray[0].Name);
        Assert.Equal(Value, lms.ClassifiedTagArray[0].Value);
        Assert.Equal(set.ToFrozenSet(), lms.ClassifiedTagArray[0].ClassificationsSet);

        lms.Clear();
        Assert.Equal(0, lms.TagsCount);

        collector.Add(PropName, Value);
        Assert.Equal(1, lms.TagsCount);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);

        collector.Add(PropName, Value, set.ToFrozenSet());
        Assert.Equal(1, lms.TagsCount);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);

        Assert.Equal(1, lms.ClassifiedTagsCount);
        Assert.Equal(PropName, lms.ClassifiedTagArray[0].Name);
        Assert.Equal(Value, lms.ClassifiedTagArray[0].Value);
        Assert.Equal(set.ToFrozenSet(), lms.ClassifiedTagArray[0].ClassificationsSet);
    }

    [Fact]
    public static void ReadOnlyListContract()
    {
        const string PropName = "Property Name";
        const string Value = "Value";

        var lms = new LoggerMessageState();
        var list = (IReadOnlyList<KeyValuePair<string, object?>>)lms;

        var index = lms.ReserveTagSpace(1);
        lms.TagArray[index] = new(PropName, Value);
        Assert.Equal(1, list.Count);
        Assert.Equal(PropName, list[0].Key);
        Assert.Equal(Value, list[0].Value);
        Assert.Equal(lms.TagArray.ToArray(), list.ToArray());

        lms.Clear();
        Assert.Empty(list);

        index = lms.ReserveTagSpace(1);
        lms.TagArray[index] = new(PropName, Value);
        Assert.Equal(1, list.Count);
        Assert.Equal(PropName, list[0].Key);
        Assert.Equal(Value, list[0].Value);

        index = lms.ReserveClassifiedTagSpace(1);
        lms.ClassifiedTagArray[index] = new(PropName, Value, new HashSet<DataClassification> { FakeClassifications.PrivateData }.ToFrozenSet());
        Assert.Equal(1, list.Count);
        Assert.Equal(PropName, list[0].Key);
        Assert.Equal(Value, list[0].Value);

        // legacy IEnumerable
        var e = (IEnumerable)list;
        var m = e.GetEnumerator();
        var count = 0;
        while (m.MoveNext())
        {
            var current = (KeyValuePair<string, object?>)m.Current!;
            Assert.Equal(current.Key, list[count].Key);
            Assert.Equal(current.Value, list[count].Value);
            count++;
        }

        Assert.Equal(1, count);
    }
}

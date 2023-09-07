﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Compliance.Testing;
using Microsoft.Extensions.Telemetry.Enrichment;
using Xunit;

namespace Microsoft.Extensions.Telemetry.Logging.Test;

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
        Assert.Equal(1, lms.NumTags);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);
        Assert.Equal("Property Name=Value", lms.ToString());

        lms.Clear();
        Assert.Equal(0, lms.NumTags);
        Assert.Equal("", lms.ToString());

        index = lms.ReserveTagSpace(1);
        lms.TagArray[index] = new(PropName, Value);
        Assert.Equal(1, lms.NumTags);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);
        Assert.Equal("Property Name=Value", lms.ToString());

        index = lms.ReserveClassifiedTagSpace(1);
        lms.ClassifiedTagArray[index] = new(PropName, Value, SimpleClassifications.PrivateData);
        Assert.Equal(1, lms.NumTags);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);
        Assert.Equal("Property Name=Value,Property Name=Microsoft.Extensions.Compliance.Testing.SimpleTaxonomy:2", lms.ToString());

        Assert.Equal(1, lms.NumClassifiedTags);
        Assert.Equal(PropName, lms.ClassifiedTagArray[0].Name);
        Assert.Equal(Value, lms.ClassifiedTagArray[0].Value);
        Assert.Equal(SimpleClassifications.PrivateData, lms.ClassifiedTagArray[0].Classification);
        Assert.Equal("Property Name=Value,Property Name=Microsoft.Extensions.Compliance.Testing.SimpleTaxonomy:2", lms.ToString());

        index = lms.ReserveTagSpace(1);
        lms.TagArray[index] = new(PropName + "X", Value);
        Assert.Equal(2, lms.NumTags);
        Assert.Equal(2, lms.TagArray.Length);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);
        Assert.Equal(PropName + "X", lms.TagArray[1].Key);
        Assert.Equal(Value, lms.TagArray[1].Value);
        Assert.Equal("Property Name=Value,Property NameX=Value,Property Name=Microsoft.Extensions.Compliance.Testing.SimpleTaxonomy:2", lms.ToString());
    }

    [Fact]
    public static void PropertyBagContract()
    {
        var lms = new LoggerMessageState();

        var collector = (IEnrichmentTagCollector)lms;

        collector.Add("K1", "V1");

        Assert.Equal(1, lms.NumTags);
        Assert.Equal(0, lms.NumClassifiedTags);

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
        Assert.Equal(1, lms.NumTags);
        Assert.Equal(PropertyNamPrefix + "_" + PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);

        collector.Add(PropName, Value, SimpleClassifications.PrivateData);
        Assert.Equal(1, lms.NumTags);
        Assert.Equal(PropertyNamPrefix + "_" + PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);

        Assert.Equal(1, lms.NumClassifiedTags);
        Assert.Equal(PropertyNamPrefix + "_" + PropName, lms.ClassifiedTagArray[0].Name);
        Assert.Equal(Value, lms.ClassifiedTagArray[0].Value);
        Assert.Equal(SimpleClassifications.PrivateData, lms.ClassifiedTagArray[0].Classification);

        lms.Clear();
        Assert.Equal(0, lms.NumTags);

        collector.Add(PropName, Value);
        Assert.Equal(1, lms.NumTags);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);

        collector.Add(PropName, Value, SimpleClassifications.PrivateData);
        Assert.Equal(1, lms.NumTags);
        Assert.Equal(PropName, lms.TagArray[0].Key);
        Assert.Equal(Value, lms.TagArray[0].Value);

        Assert.Equal(1, lms.NumClassifiedTags);
        Assert.Equal(PropName, lms.ClassifiedTagArray[0].Name);
        Assert.Equal(Value, lms.ClassifiedTagArray[0].Value);
        Assert.Equal(SimpleClassifications.PrivateData, lms.ClassifiedTagArray[0].Classification);
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
        lms.ClassifiedTagArray[index] = new(PropName, Value, SimpleClassifications.PrivateData);
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

// Licensed to the .NET Foundation under one or more agreements.
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

        var index = lms.ReservePropertySpace(1);
        lms.PropertyArray[index] = new(PropName, Value);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(PropName, lms.PropertyArray[0].Key);
        Assert.Equal(Value, lms.PropertyArray[0].Value);
        Assert.Equal("Property Name=Value", lms.ToString());

        lms.Clear();
        Assert.Equal(0, lms.NumProperties);
        Assert.Equal("", lms.ToString());

        index = lms.ReservePropertySpace(1);
        lms.PropertyArray[index] = new(PropName, Value);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(PropName, lms.PropertyArray[0].Key);
        Assert.Equal(Value, lms.PropertyArray[0].Value);
        Assert.Equal("Property Name=Value", lms.ToString());

        index = lms.ReserveClassifiedPropertySpace(1);
        lms.ClassifiedPropertyArray[index] = new(PropName, Value, SimpleClassifications.PrivateData);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(PropName, lms.PropertyArray[0].Key);
        Assert.Equal(Value, lms.PropertyArray[0].Value);
        Assert.Equal("Property Name=Value,Property Name=Microsoft.Extensions.Compliance.Testing.SimpleTaxonomy:2", lms.ToString());

        Assert.Equal(1, lms.NumClassifiedProperties);
        Assert.Equal(PropName, lms.ClassifiedPropertyArray[0].Name);
        Assert.Equal(Value, lms.ClassifiedPropertyArray[0].Value);
        Assert.Equal(SimpleClassifications.PrivateData, lms.ClassifiedPropertyArray[0].Classification);
        Assert.Equal("Property Name=Value,Property Name=Microsoft.Extensions.Compliance.Testing.SimpleTaxonomy:2", lms.ToString());

        index = lms.ReservePropertySpace(1);
        lms.PropertyArray[index] = new(PropName + "X", Value);
        Assert.Equal(2, lms.NumProperties);
        Assert.Equal(2, lms.PropertyArray.Length);
        Assert.Equal(PropName, lms.PropertyArray[0].Key);
        Assert.Equal(Value, lms.PropertyArray[0].Value);
        Assert.Equal(PropName + "X", lms.PropertyArray[1].Key);
        Assert.Equal(Value, lms.PropertyArray[1].Value);
        Assert.Equal("Property Name=Value,Property NameX=Value,Property Name=Microsoft.Extensions.Compliance.Testing.SimpleTaxonomy:2", lms.ToString());
    }

    [Fact]
    public static void PropertyBagContract()
    {
        var lms = new LoggerMessageState();

        var bag = (IEnrichmentPropertyBag)lms;

        bag.Add("K1", "V1");
        bag.Add("K2", (object)"V2");
        bag.Add(new[] { new KeyValuePair<string, string>("K3", "V3") }.AsSpan());
        bag.Add(new[] { new KeyValuePair<string, object>("K4", "V4") }.AsSpan());

        Assert.Equal(4, lms.NumProperties);
        Assert.Equal(0, lms.NumClassifiedProperties);
        Assert.Equal(0, lms.NumRedactedProperties);

        Assert.Equal("K1", lms.PropertyArray[0].Key);
        Assert.Equal("K2", lms.PropertyArray[1].Key);
        Assert.Equal("K3", lms.PropertyArray[2].Key);
        Assert.Equal("K4", lms.PropertyArray[3].Key);

        Assert.Equal("V1", lms.PropertyArray[0].Value);
        Assert.Equal("V2", lms.PropertyArray[1].Value);
        Assert.Equal("V3", lms.PropertyArray[2].Value);
        Assert.Equal("V4", lms.PropertyArray[3].Value);
    }

    [Fact]
    public static void CollectorContract()
    {
        const string PropertyNamPrefix = "param_name";
        const string PropName = "Property Name";
        const string Value = "Value";

        var lms = new LoggerMessageState();

        var collector = (ILogPropertyCollector)lms;
        lms.PropertyNamePrefix = PropertyNamPrefix;

        collector.Add(PropName, Value);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(PropertyNamPrefix + "_" + PropName, lms.PropertyArray[0].Key);
        Assert.Equal(Value, lms.PropertyArray[0].Value);

        collector.Add(PropName, Value, SimpleClassifications.PrivateData);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(PropertyNamPrefix + "_" + PropName, lms.PropertyArray[0].Key);
        Assert.Equal(Value, lms.PropertyArray[0].Value);

        Assert.Equal(1, lms.NumClassifiedProperties);
        Assert.Equal(PropertyNamPrefix + "_" + PropName, lms.ClassifiedPropertyArray[0].Name);
        Assert.Equal(Value, lms.ClassifiedPropertyArray[0].Value);
        Assert.Equal(SimpleClassifications.PrivateData, lms.ClassifiedPropertyArray[0].Classification);

        lms.Clear();
        Assert.Equal(0, lms.NumProperties);

        collector.Add(PropName, Value);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(PropName, lms.PropertyArray[0].Key);
        Assert.Equal(Value, lms.PropertyArray[0].Value);

        collector.Add(PropName, Value, SimpleClassifications.PrivateData);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(PropName, lms.PropertyArray[0].Key);
        Assert.Equal(Value, lms.PropertyArray[0].Value);

        Assert.Equal(1, lms.NumClassifiedProperties);
        Assert.Equal(PropName, lms.ClassifiedPropertyArray[0].Name);
        Assert.Equal(Value, lms.ClassifiedPropertyArray[0].Value);
        Assert.Equal(SimpleClassifications.PrivateData, lms.ClassifiedPropertyArray[0].Classification);
    }

    [Fact]
    public static void ReadOnlyListContract()
    {
        const string PropName = "Property Name";
        const string Value = "Value";

        var lms = new LoggerMessageState();
        var list = (IReadOnlyList<KeyValuePair<string, object?>>)lms;

        var index = lms.ReservePropertySpace(1);
        lms.PropertyArray[index] = new(PropName, Value);
        Assert.Equal(1, list.Count);
        Assert.Equal(PropName, list[0].Key);
        Assert.Equal(Value, list[0].Value);
        Assert.Equal(lms.PropertyArray.ToArray(), list.ToArray());

        lms.Clear();
        Assert.Empty(list);

        index = lms.ReservePropertySpace(1);
        lms.PropertyArray[index] = new(PropName, Value);
        Assert.Equal(1, list.Count);
        Assert.Equal(PropName, list[0].Key);
        Assert.Equal(Value, list[0].Value);

        index = lms.ReserveClassifiedPropertySpace(1);
        lms.ClassifiedPropertyArray[index] = new(PropName, Value, SimpleClassifications.PrivateData);
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

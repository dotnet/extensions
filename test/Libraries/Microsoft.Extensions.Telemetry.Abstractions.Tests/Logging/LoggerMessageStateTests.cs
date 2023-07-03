// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
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

        lms.AllocPropertySpace(1)[0] = new(PropName, Value);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(1, lms.Properties.Length);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        _ = lms.TryReset();
        Assert.Equal(0, lms.NumProperties);
        Assert.Equal(0, lms.Properties.Length);

        lms.AllocPropertySpace(1)[0] = new(PropName, Value);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(1, lms.Properties.Length);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        lms.AllocClassifiedPropertySpace(1)[0] = new(PropName, Value, SimpleClassifications.PrivateData);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(1, lms.Properties.Length);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        Assert.Equal(1, lms.NumClassifiedProperties);
        Assert.Equal(1, lms.ClassifiedProperties.Length);
        Assert.Equal(PropName, lms.ClassifiedProperties[0].Name);
        Assert.Equal(Value, lms.ClassifiedProperties[0].Value);
        Assert.Equal(SimpleClassifications.PrivateData, lms.ClassifiedProperties[0].Classification);
    }

    [Fact]
    public static void CollectorContract()
    {
        const string PropertyNamPrefix = "param_name_";
        const string PropName = "Property Name";
        const string Value = "Value";

        var lms = new LoggerMessageState();

        var collector = (ILogPropertyCollector)lms;
        lms.PropertyNamePrefix = PropertyNamPrefix;

        collector.Add(PropName, Value);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(1, lms.Properties.Length);
        Assert.Equal(PropertyNamPrefix + PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        _ = lms.TryReset();
        Assert.Equal(0, lms.NumProperties);
        Assert.Equal(0, lms.Properties.Length);

        collector.Add(PropName, Value);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(1, lms.Properties.Length);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        collector.Add(PropName, Value, SimpleClassifications.PrivateData);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(1, lms.Properties.Length);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        Assert.Equal(1, lms.NumClassifiedProperties);
        Assert.Equal(1, lms.ClassifiedProperties.Length);
        Assert.Equal(PropName, lms.ClassifiedProperties[0].Name);
        Assert.Equal(Value, lms.ClassifiedProperties[0].Value);
        Assert.Equal(SimpleClassifications.PrivateData, lms.ClassifiedProperties[0].Classification);
    }

    [Fact]
    public static void EnrichmentBagContract()
    {
        const string PropName = "Property Name";
        const string Value = "Value";

        var lms = new LoggerMessageState();
        var bag = (IEnrichmentPropertyBag)lms;

        bag.Add(PropName, Value);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(1, lms.Properties.Length);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        _ = lms.TryReset();
        bag.Add(PropName, (object)Value);
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(1, lms.Properties.Length);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        _ = lms.TryReset();
        bag.Add(new[] { new KeyValuePair<string, object>(PropName, Value) }.AsSpan());
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(1, lms.Properties.Length);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        _ = lms.TryReset();
        bag.Add(new[] { new KeyValuePair<string, string>(PropName, Value) }.AsSpan());
        Assert.Equal(1, lms.NumProperties);
        Assert.Equal(1, lms.Properties.Length);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);
    }
}

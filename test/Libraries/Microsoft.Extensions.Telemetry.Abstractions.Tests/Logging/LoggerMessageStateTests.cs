// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Compliance.Testing;
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

        lms.AddProperty(PropName, Value);
        Assert.Single(lms.Properties);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        _ = lms.TryReset();
        Assert.Empty(lms.Properties);

        lms.AddProperty(PropName, Value);
        Assert.Single(lms.Properties);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        lms.AddProperty(PropName, Value, SimpleClassifications.PrivateData);
        Assert.Single(lms.Properties);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        Assert.Single(lms.ClassifiedProperties);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);
        Assert.Equal(SimpleClassifications.PrivateData, lms.ClassifiedProperties[0].Classification);
    }

    [Fact]
    public static void CollectorContract()
    {
        const string ParamName = "param_name Name";
        const string PropName = "Property Name";
        const string Value = "Value";

        var lms = new LoggerMessageState();

        var collector = lms.GetPropertyCollector(ParamName);
        collector.Add(PropName, Value);
        Assert.Single(lms.Properties);
        Assert.Equal(ParamName + "_" + PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        _ = lms.TryReset();
        Assert.Empty(lms.Properties);

        collector.Add(PropName, Value);
        Assert.Single(lms.Properties);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        collector.Add(PropName, Value, SimpleClassifications.PrivateData);
        Assert.Single(lms.Properties);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);

        Assert.Single(lms.ClassifiedProperties);
        Assert.Equal(PropName, lms.Properties[0].Key);
        Assert.Equal(Value, lms.Properties[0].Value);
        Assert.Equal(SimpleClassifications.PrivateData, lms.ClassifiedProperties[0].Classification);
    }

    [Fact]
    public static void EnrichmentBagContract()
    {
        const string PropName = "Property Name";
        const string Value = "Value";

        var lmp = new LoggerMessageState();
        var bag = lmp.EnrichmentPropertyBag;

        bag.Add(PropName, Value);
        Assert.Single(lmp.Properties);
        Assert.Equal(PropName, lmp.Properties[0].Key);
        Assert.Equal(Value, lmp.Properties[0].Value);

        _ = lmp.TryReset();
        bag.Add(PropName, (object)Value);
        Assert.Single(lmp.Properties);
        Assert.Equal(PropName, lmp.Properties[0].Key);
        Assert.Equal(Value, lmp.Properties[0].Value);

        _ = lmp.TryReset();
        bag.Add(new[] { new KeyValuePair<string, object>(PropName, Value) }.AsSpan());
        Assert.Single(lmp.Properties);
        Assert.Equal(PropName, lmp.Properties[0].Key);
        Assert.Equal(Value, lmp.Properties[0].Value);

        _ = lmp.TryReset();
        bag.Add(new[] { new KeyValuePair<string, string>(PropName, Value) }.AsSpan());
        Assert.Single(lmp.Properties);
        Assert.Equal(PropName, lmp.Properties[0].Key);
        Assert.Equal(Value, lmp.Properties[0].Value);
    }
}

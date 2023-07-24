// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Gen.Logging.Model;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class LoggingMethodParameterTests
{
    [Fact]
    public void Fields_Should_BeInitialized()
    {
        var instance = new LoggingMethodParameter();
        Assert.Empty(instance.Name);
        Assert.Empty(instance.Type);
    }

    [Theory]
    [InlineData(false, false, true, false, false)]
    [InlineData(false, true, true, false, true)]
    [InlineData(true, false, true, true, false)]
    [InlineData(true, true, true, true, true)]
    public void ShouldGetLogMethodParameterInfoCorrectly(
        bool addPropertiesToLog,
        bool setLogPropertiesProvider,
        bool expectedParamIsInTemplate,
        bool expectedParamHasProperties,
        bool expectedParamHasPropsProvider)
    {
        const string PrivateDataAttributeType = "Microsoft.Extensions.Compliance.Testing.PrivateDataAtribute";

        var lp = new LoggingMethodParameter
        {
            LogPropertiesProvider = setLogPropertiesProvider
                ? new LoggingPropertyProvider(string.Empty, string.Empty)
                : null
        };

        if (addPropertiesToLog)
        {
            lp.PropertiesToLog.Add(new LoggingProperty(string.Empty, string.Empty, PrivateDataAttributeType, false, false, false, false, false, false, Array.Empty<LoggingProperty>()));
        }

        Assert.Equal(expectedParamIsInTemplate, lp.IsNormalParameter);
        Assert.Equal(expectedParamHasProperties, lp.HasProperties);
        Assert.Equal(expectedParamHasPropsProvider, lp.HasPropsProvider);
    }

    [Fact]
    public void Misc()
    {
        var lp = new LoggingMethodParameter
        {
            Name = "Foo",
            NeedsAtSign = false,
        };

        Assert.Equal(lp.Name, lp.NameWithAt);
        lp.NeedsAtSign = true;
        Assert.Equal("@" + lp.Name, lp.NameWithAt);

        lp.Type = "Foo";
        lp.IsReference = false;
        lp.IsNullable = true;
        Assert.Equal(lp.Type, lp.PotentiallyNullableType);

        lp.IsReference = false;
        lp.IsNullable = false;
        Assert.Equal(lp.Type, lp.PotentiallyNullableType);

        lp.IsReference = true;
        lp.IsNullable = false;
        Assert.Equal(lp.Type + "?", lp.PotentiallyNullableType);

        lp.IsReference = true;
        lp.IsNullable = true;
        Assert.Equal(lp.Type, lp.PotentiallyNullableType);
    }
}

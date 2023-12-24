// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Gen.Logging.Model;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class LoggingMethodParameterTests
{
    [Fact]
    public void Fields_Should_BeInitialized()
    {
        var instance = new LoggingMethodParameter();
        Assert.Empty(instance.ParameterName);
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
            TagProvider = setLogPropertiesProvider
                ? new TagProvider(string.Empty, string.Empty)
                : null
        };

        if (addPropertiesToLog)
        {
            lp.Properties.Add(new LoggingProperty
            {
                ClassificationAttributeTypes = new HashSet<string>(new[] { PrivateDataAttributeType })
            });
        }

        Assert.Equal(expectedParamIsInTemplate, lp.IsNormalParameter);
        Assert.Equal(expectedParamHasProperties, lp.HasProperties);
        Assert.Equal(expectedParamHasPropsProvider, lp.HasTagProvider);
    }

    [Fact]
    public void Misc()
    {
        var lp = new LoggingMethodParameter
        {
            ParameterName = "Foo",
            NeedsAtSign = false,
        };

        Assert.Equal(lp.ParameterName, lp.ParameterNameWithAt);
        lp.NeedsAtSign = true;
        Assert.Equal("@" + lp.ParameterName, lp.ParameterNameWithAt);

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

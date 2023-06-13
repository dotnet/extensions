// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel.DataAnnotations;
using FluentAssertions;
using Xunit;

namespace System.Cloud.DocumentDb.Test;

public class DatabaseOptionsTests
{
    [Fact]
    public void TestProperties()
    {
        TimeSpan testTimeout = new TimeSpan(0, 1, 4, 0);

        DatabaseOptions config = new()
        {
            DatabaseName = "name",
            DefaultRegionalDatabaseName = "regional name",
            PrimaryKey = "some value",
            Endpoint = new Uri("https://endpoint"),
            IdleTcpConnectionTimeout = testTimeout,
            Throughput = new(5),
        };

        Assert.Equal("name", config.DatabaseName);

        Assert.Equal("regional name", config.DefaultRegionalDatabaseName);
        Assert.Equal("some value", config.PrimaryKey);
        Assert.Equal(new Uri("https://endpoint"), config.Endpoint);
        Assert.Equal(testTimeout, config.IdleTcpConnectionTimeout);
        Assert.Equal(5, config.Throughput.Value);

        Assert.Equal(0, config.FailoverRegions.Count);
        Assert.Equal(0, config.RegionalDatabaseOptions.Count);
    }

    [Fact]
    public void TestDefaults()
    {
        DatabaseOptions config = new DatabaseOptions();

        Assert.Equal(string.Empty, config.DatabaseName);
        Throughput.Unlimited.Should().Be(config.Throughput);
        Assert.True(config.OverrideSerialization);
    }

    [Fact]
    public void TestRangeValues()
    {
        TestIdleTimeoutValue(new TimeSpan(0), false);
        TestIdleTimeoutValue(new TimeSpan(0, 9, 59), false);
        TestIdleTimeoutValue(new TimeSpan(0, 10, 00), true);
        TestIdleTimeoutValue(new TimeSpan(30, 0, 0, 0), true);
        TestIdleTimeoutValue(new TimeSpan(30, 0, 0, 1), false);
    }

    private static void TestIdleTimeoutValue(TimeSpan timeToTest, bool expectedResult)
    {
        DatabaseOptions config = new DatabaseOptions
        {
            DatabaseName = "required",
            IdleTcpConnectionTimeout = timeToTest,
            Endpoint = new Uri("https://endpoint")
        };
        bool result = Validator.TryValidateObject(
            config,
            new ValidationContext(config, null, null),
            null,
            true);
        Assert.Equal(expectedResult, result);
    }
}

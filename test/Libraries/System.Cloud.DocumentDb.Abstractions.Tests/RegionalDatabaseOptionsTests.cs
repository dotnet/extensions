// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Cloud.DocumentDb.Test;

public class RegionalDatabaseOptionsTests
{
    [Fact]
    public void TestProperties()
    {
        RegionalDatabaseOptions config = new RegionalDatabaseOptions
        {
            DatabaseName = "name",
            PrimaryKey = "key",
            Endpoint = new Uri("https://endpoint")
        };

        Assert.Equal("name", config.DatabaseName);
        Assert.Equal(new Uri("https://endpoint"), config.Endpoint);
        Assert.Equal("key", config.PrimaryKey);

        Assert.Equal(0, config.FailoverRegions.Count);
    }
}

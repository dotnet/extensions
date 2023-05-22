// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Xunit;

namespace System.Cloud.DocumentDb.Test;

public class TableOptionsTests
{
    [Fact]
    public void TestProperties()
    {
        const string StoreName = "store name";
        TimeSpan timeToLive = new TimeSpan(1);
        const string PartitionIdPath = "partition";

        TableOptions options = new()
        {
            TableName = StoreName,
            TimeToLive = timeToLive,
            PartitionIdPath = PartitionIdPath,
            IsRegional = true,
            IsLocatorRequired = true,
            Throughput = new(5),
        };

        Assert.Equal(StoreName, options.TableName);
        Assert.Equal(timeToLive, options.TimeToLive);
        Assert.Equal(PartitionIdPath, options.PartitionIdPath);
        Assert.True(options.IsRegional);
        Assert.True(options.IsLocatorRequired);
        Assert.Equal(5, options.Throughput.Value);
    }

    [Fact]
    public void TestDefaults()
    {
        TableOptions config = new TableOptions();

        Assert.Equal(Timeout.InfiniteTimeSpan, config.TimeToLive);
        Assert.Equal(string.Empty, config.TableName);

        TableInfo info = new TableInfo(config);

        info = new TableInfo(info);
        Assert.False(info.IsRegional);
        Assert.Equal(string.Empty, info.TableName);

        info = new TableInfo(info, "123", true);
        Assert.True(info.IsRegional);
        Assert.Equal("123", info.TableName);
    }
}

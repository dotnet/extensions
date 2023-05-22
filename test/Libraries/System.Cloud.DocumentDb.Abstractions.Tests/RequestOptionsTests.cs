// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Cloud.DocumentDb.Test;

public class RequestOptionsTests
{
    [Fact]
    public void TestProperties()
    {
        QueryRequestOptions<int> options = new QueryRequestOptions<int>
        {
            PartitionKey = new[] { "partition" },
            ConsistencyLevel = ConsistencyLevel.Eventual,
            SessionToken = "session",
            ItemVersion = "etag",
            Document = 5,
            Region = "region",
            ResponseContinuationTokenLimitInKb = 6,
            ContentResponseOnWrite = true,
            EnableScan = true,
            EnableLowPrecisionOrderBy = true,
            MaxBufferedItemCount = 7,
            MaxResults = 8,
            MaxConcurrency = 9,
            ContinuationToken = "10",
        };

        Assert.Equal("partition", options.PartitionKey[0]);
        Assert.Equal("session", options.SessionToken);
        Assert.Equal("etag", options.ItemVersion);
        Assert.Equal(5, options.Document);
        Assert.Equal(ConsistencyLevel.Eventual, options.ConsistencyLevel);
        Assert.Equal("region", options.Region);

        Assert.Equal(6, options.ResponseContinuationTokenLimitInKb);
        Assert.True(options.ContentResponseOnWrite);
        Assert.True(options.EnableScan);
        Assert.True(options.EnableLowPrecisionOrderBy);
        Assert.Equal(7, options.MaxBufferedItemCount);
        Assert.Equal(8, options.MaxResults);
        Assert.Equal(9, options.MaxConcurrency);
        Assert.Equal("10", options.ContinuationToken);
        Assert.Equal(FetchMode.FetchAll, options.FetchCondition);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Cloud.DocumentDb.Test;

public class BatchItemTests
{
    [Fact]
    public void TestProperties()
    {
        BatchItem<int> item = new(
            BatchOperation.Delete,
            5,
            "id",
            "etag");

        Assert.Equal(BatchOperation.Delete, item.Operation);
        Assert.Equal("etag", item.ItemVersion);
        Assert.Equal("id", item.Id);
        Assert.Equal(5, item.Item);
    }

    [Fact]
    public void TestDefaults()
    {
        BatchItem<string> item = new(BatchOperation.Delete);

        Assert.Equal(BatchOperation.Delete, item.Operation);
        Assert.Null(item.ItemVersion);
        Assert.Null(item.Id);
        Assert.Null(item.Item);
    }
}

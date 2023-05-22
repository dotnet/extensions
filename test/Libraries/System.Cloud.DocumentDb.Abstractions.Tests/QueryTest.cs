// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Cloud.DocumentDb.Test;

public class QueryTest
{
    [Fact]
    public void TestProperties()
    {
        const string QueryText = "query";

        Query query = new Query(QueryText);

        Assert.Equal(QueryText, query.QueryText);
        Assert.Equal(0, query.Parameters.Count);
    }
}

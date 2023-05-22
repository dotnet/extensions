// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Microsoft.Shared.Collections.Test;

public class EmptyTests
{
    [Fact]
    public void Basic()
    {
        Assert.Empty(Empty.ReadOnlyCollection<int>());
        Assert.Empty(Empty.ReadOnlyList<int>());
        Assert.Empty(Empty.Enumerable<int>());
        Assert.Empty(Empty.ReadOnlyDictionary<int, int>());
    }
}

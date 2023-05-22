// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Extensions.Hosting.Testing.Internal;
using Xunit;

namespace Microsoft.Extensions.Hosting.Testing.Test;

public class FakeConfigurationSourceTest
{
    [Fact]
    public void Constructor_KeyValuePairsGiven_PopulatesInitialData()
    {
        var configSource = new FakeConfigurationSource(
            new KeyValuePair<string, string?>("testKey", "testValue"),
            new KeyValuePair<string, string?>("anotherTestKey", "anotherTestValue"));

        Assert.Collection(
            configSource.InitialData!,
            item =>
            {
                Assert.Equal("testKey", item.Key);
                Assert.Equal("testValue", item.Value);
            },
            item =>
            {
                Assert.Equal("anotherTestKey", item.Key);
                Assert.Equal("anotherTestValue", item.Value);
            });
    }

    [Fact]
    public void Constructor_NoKeyValuePairGiven_HasEmptyInitialData()
    {
        var configSource = new FakeConfigurationSource();

        Assert.Empty(configSource.InitialData!);
    }
}

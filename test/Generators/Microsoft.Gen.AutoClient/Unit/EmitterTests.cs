// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Http.AutoClient;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.AutoClient.Test;

public class EmitterTests
{
    [Fact]
    public async Task TestEmitter()
    {
        var sources = new List<string>();
        foreach (var file in Directory.GetFiles("TestClasses"))
        {
            sources.Add(File.ReadAllText(file));
        }

        var (d, r) = await RoslynTestUtils.RunGenerator(
            new AutoClientGenerator(),
            new[]
            {
                Assembly.GetAssembly(typeof(AutoClientAttribute))!,
                Assembly.GetAssembly(typeof(GetAttribute))!
            },
            sources)
            .ConfigureAwait(false);

        Assert.Empty(d);
        Assert.Single(r);

        var goldenClient = File.ReadAllText("GoldenFiles/Microsoft.Gen.AutoClient/Microsoft.Gen.AutoClient.AutoClientGenerator/AutoClients.g.cs");
        var result = r[0].SourceText.ToString();
        Assert.Equal(goldenClient, result);
    }
}

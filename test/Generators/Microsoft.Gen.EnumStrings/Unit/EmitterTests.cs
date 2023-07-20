// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.EnumStrings;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.EnumStrings.Test;

public class EmitterTests
{
    [Fact]
    public async Task TestEmitter()
    {
        var sources = new List<string>();
        foreach (var file in Directory.GetFiles("TestClasses"))
        {
            sources.Add("#define NETCOREAPP3_1_OR_GREATER\n" + File.ReadAllText(file));
        }

        // try it without the frozen collections
        var (d, r) = await RoslynTestUtils.RunGenerator(
            new EnumStringsGenerator(),
            new[]
            {
                Assembly.GetAssembly(typeof(EnumStringsAttribute))!,
            },
            sources).ConfigureAwait(false);

        Assert.Empty(d);
        _ = Assert.Single(r);

        // try it again with the frozen collections, this is what we need to compare with the golden files
        (d, r) = await RoslynTestUtils.RunGenerator(
            new EnumStringsGenerator(),
            new[]
            {
                Assembly.GetAssembly(typeof(EnumStringsAttribute))!,
                Assembly.GetAssembly(typeof(FrozenDictionary))!,
            },
            sources).ConfigureAwait(false);

        Assert.Empty(d);
        _ = Assert.Single(r);

        var golden = File.ReadAllText($"GoldenFiles/Microsoft.Gen.EnumStrings/Microsoft.Gen.EnumStrings.EnumStringsGenerator/EnumStrings.g.cs");
        var result = r[0].SourceText.ToString();
        Assert.Equal(golden, result);
    }
}

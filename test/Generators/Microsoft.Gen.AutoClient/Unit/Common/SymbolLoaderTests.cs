// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.Gen.AutoClient.Test;

public class SymbolLoaderTests
{
    [Fact]
    public void RestApiAttributeNotAvailable()
    {
        var comp = CSharpCompilation.Create(null);
        Assert.Null(SymbolLoader.LoadSymbols(comp));
    }
}

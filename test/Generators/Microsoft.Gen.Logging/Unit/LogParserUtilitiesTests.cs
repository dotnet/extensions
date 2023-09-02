// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.Gen.Shared;
using Moq;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class LogParserUtilitiesTests
{
    [Fact]
    public void ShouldDetectNullableOfT()
    {
        var typeSymbolMock = new Mock<ITypeSymbol>();
        typeSymbolMock.SetupGet(x => x.SpecialType).Returns(SpecialType.System_Nullable_T);
        var result = typeSymbolMock.Object.IsNullableOfT();
        Assert.True(result);

        var anotherTypeSymbolMock = new Mock<ITypeSymbol>();
        anotherTypeSymbolMock.SetupGet(x => x.SpecialType).Returns(SpecialType.None);
        anotherTypeSymbolMock.SetupGet(x => x.OriginalDefinition).Returns(typeSymbolMock.Object);
        result = typeSymbolMock.Object.IsNullableOfT();
        Assert.True(result);
    }

    [Fact]
    public void ShouldNotDetectNullableOfT()
    {
        var typeSymbolMock = new Mock<ITypeSymbol>();
        typeSymbolMock.SetupGet(x => x.SpecialType).Returns(SpecialType.None);
        typeSymbolMock.SetupGet(x => x.OriginalDefinition).Returns(typeSymbolMock.Object);
        var result = typeSymbolMock.Object.IsNullableOfT();
        Assert.False(result);
    }
}

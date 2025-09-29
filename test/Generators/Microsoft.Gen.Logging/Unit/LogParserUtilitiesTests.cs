// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.Logging.Model;
using Microsoft.Gen.Logging.Parsing;
using Microsoft.Gen.Shared;
using Moq;
using Moq.Protected;
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

    [Fact]
    public void RecordHasSensitivePublicMembers_ShouldReturnFalse_WhenNoDataClasses()
    {
        var symbolMock = new Mock<ITypeSymbol>();
        symbolMock
            .Setup(x => x.GetMembers())
            .Returns(ImmutableArray<ISymbol>.Empty);

        var symbolHolder = new SymbolHolder(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

        var diagMock = new Mock<Action<Diagnostic>>();
        var parser = new Parser(null!, diagMock.Object, CancellationToken.None);
        var result = parser.RecordHasSensitivePublicMembers(symbolMock.Object, symbolHolder);
        Assert.False(result);
        symbolMock.VerifyNoOtherCalls();
    }

    [Theory]
    [CombinatorialData]
    public void RecordHasSensitivePublicMembers_ShouldNotThrow_WhenNoMembersOnType(bool isNull)
    {
        var symbolMock = new Mock<INamedTypeSymbol>();
        symbolMock
            .Setup(x => x.GetMembers())
            .Returns(isNull
                ? default
                : ImmutableArray<ISymbol>.Empty);

        symbolMock
            .Setup(x => x.GetAttributes())
            .Returns(Array.Empty<AttributeData>().ToImmutableArray());

        symbolMock
            .SetupGet(x => x.BaseType)
            .Returns((INamedTypeSymbol?)null);

        symbolMock
            .SetupGet(x => x.SpecialType)
            .Returns(SpecialType.None);

        var symbolHolder = new SymbolHolder(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            Mock.Of<INamedTypeSymbol>(),
            null!);

        var diagMock = new Mock<Action<Diagnostic>>();
        var parser = new Parser(null!, diagMock.Object, CancellationToken.None);
        var result = parser.RecordHasSensitivePublicMembers(symbolMock.Object, symbolHolder);
        Assert.False(result);
        symbolMock.VerifyAll();
        symbolMock.VerifyNoOtherCalls();
    }

    [Theory]
    [CombinatorialData]
    public void ProcessLogPropertiesForParameter_ShouldNotThrow_WhenNoMembersOnType(bool isNull)
    {
        var symbolHolder = new SymbolHolder(
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default),
            null!,
            null!,
            null!,
            null!,
            null!);

        const string ParamType = "param type";

        var paramTypeMock = new Mock<INamedTypeSymbol>()
            .As<ITypeSymbol>();

        paramTypeMock.SetupGet(x => x.Kind).Returns(SymbolKind.NamedType);
        paramTypeMock.SetupGet(x => x.TypeKind).Returns(TypeKind.Class);
        paramTypeMock.SetupGet(x => x.SpecialType).Returns(SpecialType.None);
        paramTypeMock.SetupGet(x => x.OriginalDefinition).Returns(paramTypeMock.Object);
        paramTypeMock.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>()))
            .Returns(ParamType);

        paramTypeMock
            .SetupGet(x => x.BaseType)
            .Returns((INamedTypeSymbol?)null);

        paramTypeMock
            .Setup(x => x.GetMembers())
            .Returns(isNull
                ? default
                : ImmutableArray<ISymbol>.Empty);

        var paramSymbolMock = new Mock<IParameterSymbol>();
        paramSymbolMock.SetupGet(x => x.Type)
            .Returns(paramTypeMock.Object);

        var logPropertiesAttribute = new Mock<AttributeData>();
        logPropertiesAttribute
            .Protected()
            .SetupGet<ImmutableArray<KeyValuePair<string, TypedConstant>>>("CommonNamedArguments")
            .Returns(ImmutableArray<KeyValuePair<string, TypedConstant>>.Empty);

        logPropertiesAttribute
            .Protected()
            .SetupGet<ImmutableArray<TypedConstant>>("CommonConstructorArguments")
            .Returns(ImmutableArray<TypedConstant>.Empty);

        var diagMock = new Mock<Action<Diagnostic>>();
        var parser = new Parser(null!, diagMock.Object, CancellationToken.None);
        bool unused = false;
        var result = parser.ProcessLogPropertiesForParameter(
            logPropertiesAttribute.Object,
            null!,
            new LoggingMethodParameter(),
            paramSymbolMock.Object,
            symbolHolder,
            ref unused);

        diagMock.Verify(x => x.Invoke(It.Is<Diagnostic>(d => d.Id == DiagDescriptors.LogPropertiesParameterSkipped.Id)), Times.Once);
        Assert.True(result);
    }
}

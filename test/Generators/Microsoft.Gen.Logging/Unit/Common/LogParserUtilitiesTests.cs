// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
    [Theory]
    [InlineData(false, false, false, true)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(true, false, false, false)]
    public void ShouldSkipLoggingMethodWhenParameterIsSpecial(bool isLogger, bool isRedactorProvider, bool isException, bool isLogLevel)
    {
        const string ParamName = "param name";

        var paramSymbolMock = new Mock<IParameterSymbol>();
        paramSymbolMock.SetupGet(x => x.Name)
            .Returns(ParamName);

        var loggerParameter = new LoggingMethodParameter
        {
            IsLogger = isLogger,
            IsRedactorProvider = isRedactorProvider,
            IsException = isException,
            IsLogLevel = isLogLevel
        };

        var diagMock = new Mock<Action<DiagnosticDescriptor, Location?, object?[]?>>();
        var result = LogParserUtilities.ProcessLogPropertiesForParameter(null!, null!, loggerParameter, paramSymbolMock.Object, null!, diagMock.Object, null!, CancellationToken.None);

        Assert.True(result == LogPropertiesProcessingResult.Fail);
        diagMock.Verify(
            x => x(It.IsAny<DiagnosticDescriptor>(), It.IsAny<Location?>(), It.Is<object?[]?>(p => p != null && p.Length == 1 && Equals(p[0], ParamName))),
            Times.Once);

        diagMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void ShouldSkipLoggingMethodWhenParameterTypeIsSpecial()
    {
        const string ParamType = "param type";

        var paramTypeMock = new Mock<ITypeSymbol>();
        paramTypeMock.SetupGet(x => x.Kind).Returns(SymbolKind.NamedType);
        paramTypeMock.SetupGet(x => x.TypeKind).Returns(TypeKind.Class);
        paramTypeMock.SetupGet(x => x.SpecialType).Returns(SpecialType.System_Array);
        paramTypeMock.SetupGet(x => x.OriginalDefinition).Returns(paramTypeMock.Object);
        paramTypeMock.Setup(x => x.ToDisplayString(It.IsAny<SymbolDisplayFormat>()))
            .Returns(ParamType);

        var paramSymbolMock = new Mock<IParameterSymbol>();
        paramSymbolMock.SetupGet(x => x.Type)
            .Returns(paramTypeMock.Object);

        var diagMock = new Mock<Action<DiagnosticDescriptor, Location?, object?[]?>>();
        var result = LogParserUtilities.ProcessLogPropertiesForParameter(null!, null!, new LoggingMethodParameter(), paramSymbolMock.Object, null!, diagMock.Object, null!, CancellationToken.None);

        Assert.True(result == LogPropertiesProcessingResult.Fail);
        diagMock.Verify(
            x => x(It.IsAny<DiagnosticDescriptor>(), It.IsAny<Location?>(), It.Is<object?[]?>(p => p != null && p.Length == 1 && Equals(p[0], ParamType))),
            Times.Once);

        diagMock.VerifyNoOtherCalls();
    }

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
    public void ShouldGet_DataClassificationAttr_WhenAttrClassIsNull()
    {
        var attributeMock = new Mock<AttributeData>();
        attributeMock
            .Protected()
            .SetupGet<INamedTypeSymbol?>("CommonAttributeClass")
            .Returns((INamedTypeSymbol?)null);

        var symbolMock = new Mock<ISymbol>();
        symbolMock.Setup(x => x.GetAttributes())
            .Returns(new[] { attributeMock.Object }.ToImmutableArray());

        var result = LogParserUtilities.GetDataClassificationAttributes(symbolMock.Object, null!);
        Assert.Empty(result);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.Logging.Parsing;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class SymbolLoaderTests
{
    [Theory]
    [InlineData(SymbolLoader.LogMethodAttribute)]
    [InlineData(SymbolLoader.LogLevelType)]
    [InlineData(SymbolLoader.ILoggerType)]
    [InlineData(SymbolLoader.ExceptionType, true)]
    [InlineData(SymbolLoader.LogPropertiesAttribute)]
    [InlineData(SymbolLoader.ITagCollectorType)]
    [InlineData(SymbolLoader.LogPropertyIgnoreAttribute)]
    public void Loader_ReturnsNull_WhenTypeIsUnavailable(string type, bool callbackShouldBeCalled = false)
    {
        var compilationMock = new Mock<Compilation>(
            string.Empty,
            Array.Empty<MetadataReference>().ToImmutableArray(),
            new Dictionary<string, string>(),
            false,
            null!,
            null!);

        compilationMock
            .Protected()
            .Setup<INamedTypeSymbol>("CommonGetTypeByMetadataName", ItExpr.Is<string>(t => t != type))
            .Returns(Mock.Of<INamedTypeSymbol>());

        compilationMock
            .Protected()
            .Setup<INamedTypeSymbol?>("CommonGetTypeByMetadataName", ItExpr.Is<string>(t => t == type))
            .Returns((INamedTypeSymbol?)null);

        var callbackMock = new Mock<Action<DiagnosticDescriptor, Location?, object?[]?>>();
        var result = SymbolLoader.LoadSymbols(compilationMock.Object, callbackMock.Object);
        Assert.Null(result);
        if (callbackShouldBeCalled)
        {
            callbackMock.Verify(
                x => x(It.IsAny<DiagnosticDescriptor>(), It.IsAny<Location?>(), It.Is<object?[]?>(p => p != null && p.Length > 0)),
                Times.Once);
        }

        callbackMock.VerifyNoOtherCalls();
    }
}

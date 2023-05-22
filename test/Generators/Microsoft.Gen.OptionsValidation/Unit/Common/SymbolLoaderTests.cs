// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Moq;
using Moq.Protected;
using Xunit;

namespace Microsoft.Gen.OptionsValidation.Test;

public class SymbolLoaderTests
{
    [Theory]
    [InlineData(SymbolLoader.OptionsValidatorAttribute)]
    [InlineData(SymbolLoader.ValidationAttribute)]
    [InlineData(SymbolLoader.DataTypeAttribute)]
    [InlineData(SymbolLoader.IValidatableObjectType)]
    [InlineData(SymbolLoader.IValidateOptionsType)]
    [InlineData(SymbolLoader.TypeOfType)]
    public void Loader_ReturnsFalse_WhenRequiredTypeIsUnavailable(string type)
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

        var callbackMock = new Mock<Action<Diagnostic>>();
        var result = SymbolLoader.TryLoad(compilationMock.Object, out var holder);
        Assert.False(result);
        Assert.Null(holder);
        callbackMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(SymbolLoader.LegacyValidateTransitivelyAttribute)]
    [InlineData(SymbolLoader.ValidateObjectMembersAttribute)]
    [InlineData(SymbolLoader.ValidateEnumeratedItemsAttribute)]
    public void Loader_ReturnsTrue_WhenOptionalTypeIsUnavailable(string type)
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

        var callbackMock = new Mock<Action<Diagnostic>>();
        var result = SymbolLoader.TryLoad(compilationMock.Object, out var holder);
        Assert.True(result);
        Assert.NotNull(holder);
        callbackMock.VerifyNoOtherCalls();
    }
}

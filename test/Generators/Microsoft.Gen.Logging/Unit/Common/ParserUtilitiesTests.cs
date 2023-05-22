// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Gen.Shared;
using Moq;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class ParserUtilitiesTests
{
    [Fact]
    public void ShouldDetect_SymbolHasModifier()
    {
        var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
            default,
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                SyntaxFactory.Token(SyntaxKind.PartialKeyword)),
            SyntaxFactory.ParseTypeName("string"),
            null!,
            SyntaxFactory.Identifier("Identifier_1"),
            null!);

        var anotherPropertyDeclaration = SyntaxFactory.PropertyDeclaration(
            default,
            SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                SyntaxFactory.Token(SyntaxKind.VirtualKeyword)),
            SyntaxFactory.ParseTypeName("object"),
            null!,
            SyntaxFactory.Identifier("Identifier_2"),
            null!);

        var syntaxReferenceMock = new Mock<SyntaxReference>();
        syntaxReferenceMock.Setup(x => x.GetSyntax(It.IsAny<CancellationToken>()))
            .Returns(propertyDeclaration);

        var anotherSyntaxReferenceMock = new Mock<SyntaxReference>();
        anotherSyntaxReferenceMock.Setup(x => x.GetSyntax(It.IsAny<CancellationToken>()))
            .Returns(anotherPropertyDeclaration);

        var symbolMock = new Mock<ISymbol>();
        symbolMock
            .SetupGet(x => x.DeclaringSyntaxReferences)
            .Returns(new[] { syntaxReferenceMock.Object, anotherSyntaxReferenceMock.Object }.ToImmutableArray());

        var result = ParserUtilities.PropertyHasModifier(symbolMock.Object, SyntaxKind.ProtectedKeyword, CancellationToken.None);
        Assert.True(result);
    }

    [Fact]
    public void ShouldDetect_SymbolHasNoModifier()
    {
        var propertyDeclaration = SyntaxFactory.FieldDeclaration(
            default,
            SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PartialKeyword)),
            SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("string")));

        var syntaxReferenceMock = new Mock<SyntaxReference>();
        syntaxReferenceMock.Setup(x => x.GetSyntax(It.IsAny<CancellationToken>()))
            .Returns(propertyDeclaration);

        var symbolMock = new Mock<ISymbol>();
        symbolMock
            .SetupGet(x => x.DeclaringSyntaxReferences)
            .Returns(new[] { syntaxReferenceMock.Object }.ToImmutableArray());

        var result = ParserUtilities.PropertyHasModifier(symbolMock.Object, SyntaxKind.ProtectedKeyword, CancellationToken.None);
        Assert.False(result);
    }

    [Fact]
    public void ShouldGetSymbolAttributeWhenSymbolNull()
    {
        var result = ParserUtilities.GetSymbolAttributeAnnotationOrDefault(null, null!);
        Assert.Null(result);
    }

    [Fact]
    public void Should_ReturnNull_GetLocation()
    {
        Assert.Null(ParserUtilities.GetLocation(null!));

        var symbolMock = new Mock<ISymbol>();
        symbolMock.SetupGet(x => x.Locations)
            .Returns(default(ImmutableArray<Location>));

        Assert.Null(ParserUtilities.GetLocation(symbolMock.Object));

        symbolMock.SetupGet(x => x.Locations)
            .Returns(ImmutableArray<Location>.Empty);

        Assert.Null(ParserUtilities.GetLocation(symbolMock.Object));
    }

    [Fact]
    public void Should_ReturnFirstLocation_GetLocation()
    {
        var symbolMock = new Mock<ISymbol>();
        var locationMock = Mock.Of<Location>();
        symbolMock.SetupGet(x => x.Locations)
            .Returns(new[] { locationMock, Mock.Of<Location>() }.ToImmutableArray());

        var result = ParserUtilities.GetLocation(symbolMock.Object);
        Assert.Same(locationMock, result);
    }
}

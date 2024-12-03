﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.Logging.Parsing;
using Moq;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class TypeSymbolExtensionsTests
{
    private readonly Action<DiagnosticDescriptor, Location?, object
        ?[]?> _diagCallback = (_, __, ___) => { };

    [Theory]
    [InlineData("TestEnumerableInt : List<int>", "TestEnumerableInt", true)]
    [InlineData("TestEnumerable<T> : List<T>", "TestEnumerable<object>", true)]
    [InlineData("NotUsed", "IEnumerable<string>", true)]
    [InlineData("TestClass", "NonEnumerable", false)]
    [InlineData("TestClassDerived : NonEnumerable", "TestClassDerived", false)]
    [InlineData("NotUsed", "bool", false)]
    public void ValidateIsEnumerable(string classDefinition, string typeReference, bool expectedResult)
    {
        // Generate the code
        string source = $@"
                namespace Test
                {{
                    using System.Collections.Generic;
                    using Microsoft.Extensions.Logging;

                    class {classDefinition} {{ }}

                    class NonEnumerable {{ }}

                    partial class C
                    {{
                        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger, {typeReference} property);
                    }}
                }}";

        // Create compilation and extract symbols
        Compilation compilation = CompilationHelper.CreateCompilation(source);
        SymbolHolder? symbolHolder = SymbolLoader.LoadSymbols(compilation, _diagCallback);

        IEnumerable<ISymbol> methodSymbols = compilation.GetSymbolsWithName("M1", SymbolFilter.Member);

        // Assert
        Assert.NotNull(symbolHolder);
        ISymbol symbol = Assert.Single(methodSymbols);
        var methodSymbol = Assert.IsAssignableFrom<IMethodSymbol>(symbol);
        var parameterSymbol = Assert.Single(methodSymbol.Parameters, p => p.Name == "property");

        Assert.Equal(expectedResult, parameterSymbol.Type.IsEnumerable(symbolHolder));
    }

    [Theory]
    [InlineData("TestFormattable", "TestFormattable", true)]
    [InlineData("TestFormattable : IFormattable", "TestFormattable", true)]
    [InlineData("TestFormattable", "NonFormattable", false)]
    public void ValidateImplementsIFormattable(string classDefinition, string typeReference, bool expectedResult)
    {
        // Generate the code
        string source = $@"
                namespace Test
                {{
                    using System;
                    using Microsoft.Extensions.Logging;

                    class {classDefinition} 
                    {{
                        public string ToString(string? format, IFormatProvider? formatProvider)
                        {{
                            throw new NotImplementedException();
                        }}
                    }}

                    class NonFormattable {{ }}

                    partial class C
                    {{
                        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger, {typeReference} property);
                    }}
                }}";

        // Create compilation and extract symbols
        Compilation compilation = CompilationHelper.CreateCompilation(source);
        SymbolHolder? symbolHolder = SymbolLoader.LoadSymbols(compilation, _diagCallback);
        IEnumerable<ISymbol> methodSymbols = compilation.GetSymbolsWithName("M1", SymbolFilter.Member);

        // Assert
        Assert.NotNull(symbolHolder);
        ISymbol symbol = Assert.Single(methodSymbols);
        var methodSymbol = Assert.IsAssignableFrom<IMethodSymbol>(symbol);
        var parameterSymbol = Assert.Single(methodSymbol.Parameters, p => p.Name == "property");

        Assert.Equal(expectedResult, parameterSymbol.Type.ImplementsIFormattable(symbolHolder));
    }

    [Theory]
    [InlineData("TestConvertible", "TestConvertible", true)]
    [InlineData("TestConvertible : IConvertible", "TestConvertible", true)]
    [InlineData("TestConvertible", "NonConvertible", false)]
    public void ValidateImplementsIConvertible(string classDefinition, string typeReference, bool expectedResult)
    {
        // Generate the code
        string source = $@"
                namespace Test
                {{
                    using System;
                    using Microsoft.Extensions.Logging;

                    class {classDefinition} 
                    {{
                        public string ToString(IFormatProvider? formatProvider)
                        {{
                            throw new NotImplementedException();
                        }}
                    }}

                    class NonConvertible {{ }}

                    partial class C
                    {{
                        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger, {typeReference} property);
                    }}
                }}";

        // Create compilation and extract symbols
        Compilation compilation = CompilationHelper.CreateCompilation(source);
        SymbolHolder? symbolHolder = SymbolLoader.LoadSymbols(compilation, _diagCallback);
        IEnumerable<ISymbol> methodSymbols = compilation.GetSymbolsWithName("M1", SymbolFilter.Member);

        // Assert
        Assert.NotNull(symbolHolder);
        ISymbol symbol = Assert.Single(methodSymbols);
        var methodSymbol = Assert.IsAssignableFrom<IMethodSymbol>(symbol);
        var parameterSymbol = Assert.Single(methodSymbol.Parameters, p => p.Name == "property");

        Assert.Equal(expectedResult, parameterSymbol.Type.ImplementsIConvertible(symbolHolder));
    }

    [Theory]
    [InlineData("TestISpanFormattable : ISpanFormattable", "TestISpanFormattable", true)]
    [InlineData("TestISpanFormattable", "NonConvertible", false)]
    public void ValidateImplementsISpanFormattable(string classDefinition, string typeReference, bool expectedResult)
    {
        // Generate the code
        string source = $@"
                namespace Test
                {{
                    using System;
                    using Microsoft.Extensions.Logging;

                    class {classDefinition} 
                    {{
                        public string ToString(string? format, IFormatProvider? formatProvider)
                        {{
                            throw new NotImplementedException();
                        }}

                        public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)
                        {{
                            throw new NotImplementedException();
                        }}
                    }}

                    class NonSpanFormattable {{ }}

                    partial class C
                    {{
                        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger, {typeReference} property);
                    }}
                }}";

        // Create compilation and extract symbols
        Compilation compilation = CompilationHelper.CreateCompilation(source);
        SymbolHolder? symbolHolder = SymbolLoader.LoadSymbols(compilation, _diagCallback);
        IEnumerable<ISymbol> methodSymbols = compilation.GetSymbolsWithName("M1", SymbolFilter.Member);

        // Assert
        Assert.NotNull(symbolHolder);
        ISymbol symbol = Assert.Single(methodSymbols);
        var methodSymbol = Assert.IsAssignableFrom<IMethodSymbol>(symbol);
        var parameterSymbol = Assert.Single(methodSymbol.Parameters, p => p.Name == "property");

        Assert.Equal(expectedResult, parameterSymbol.Type.ImplementsISpanFormattable(symbolHolder));
    }

    [Theory]
    [InlineData("string", true)]
    [InlineData("bool", true)]
    [InlineData("int", true)]
    [InlineData("NonSpecialType", false)]
    [InlineData("TestClassDerived", false)]
    [InlineData("TimeSpan", false)]
    [InlineData("Uri", false)]
    public void ValidateIsSpecialType(string typeReference, bool expectedResult)
    {
        // Generate the code
        string source = $@"
                namespace Test
                {{
                    using System.Collections.Generic;
                    using Microsoft.Extensions.Logging;

                    class NonSpecialType {{ }}

                    class TestClassDerived: NonSpecialType {{ }}

                    partial class C
                    {{
                        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger, {typeReference} property);
                    }}
                }}";

        // Create compilation and extract symbols
        Compilation compilation = CompilationHelper.CreateCompilation(source);
        SymbolHolder? symbolHolder = SymbolLoader.LoadSymbols(compilation, _diagCallback);

        IEnumerable<ISymbol> methodSymbols = compilation.GetSymbolsWithName("M1", SymbolFilter.Member);

        // Assert
        Assert.NotNull(symbolHolder);
        ISymbol symbol = Assert.Single(methodSymbols);
        var methodSymbol = Assert.IsAssignableFrom<IMethodSymbol>(symbol);
        var parameterSymbol = Assert.Single(methodSymbol.Parameters, p => p.Name == "property");

        Assert.Equal(expectedResult, parameterSymbol.Type.IsSpecialType(symbolHolder));
    }

    [Theory]
    [InlineData("ToString", "Test", true)]
    [InlineData("RandomMethod", "Test", false)]
    [InlineData("ToooooString", "Test", false)]
    public void ValidateHasCustomToString(string methodName, string typeReference, bool expectedResult)
    {
        // Generate the code
        string source = $@"
                namespace Test
                {{
                    using System;
                    using Microsoft.Extensions.Logging;

                    class {typeReference} 
                    {{
                        public override string {methodName}()
                        {{
                            throw new NotImplementedException();
                        }}
                    }}

                    class NonConvertible {{ }}

                    partial class C
                    {{
                        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger, {typeReference} property);

                        public override string {methodName}()
                        {{
                            throw new NotImplementedException();
                        }}
                    }}
                }}";

        // Create compilation and extract symbols
        Compilation compilation = CompilationHelper.CreateCompilation(source);
        SymbolHolder? symbolHolder = SymbolLoader.LoadSymbols(compilation, _diagCallback);
        IEnumerable<ISymbol> methodSymbols = compilation.GetSymbolsWithName("M1", SymbolFilter.Member);

        // Assert
        Assert.NotNull(symbolHolder);
        ISymbol symbol = Assert.Single(methodSymbols);
        var methodSymbol = Assert.IsAssignableFrom<IMethodSymbol>(symbol);
        var parameterSymbol = Assert.Single(methodSymbol.Parameters, p => p.Name == "property");

        Assert.Equal(expectedResult, parameterSymbol.Type.HasCustomToString());
    }

    [Fact]
    public void GetPossiblyNullWrappedType_NonNullableType_ReturnsSameType()
    {
        var typeSymbolMock = new Mock<ITypeSymbol>();

        var result = typeSymbolMock.Object.GetPossiblyNullWrappedType();

        Assert.Equal(typeSymbolMock.Object, result);
    }

    [Fact]
    public void GetPossiblyNullWrappedType_NonGenericNullableType_ReturnsSameType()
    {
        var namedTypeSymbolMock = new Mock<INamedTypeSymbol>();
        namedTypeSymbolMock.Setup(s => s.IsGenericType).Returns(false);

        var result = namedTypeSymbolMock.Object.GetPossiblyNullWrappedType();

        Assert.Equal(namedTypeSymbolMock.Object, result);
    }

    [Fact]
    public void GetPossiblyNullWrappedType_NonNullableGenericType_ReturnsSameType()
    {
        var namedTypeSymbolMock = new Mock<INamedTypeSymbol>();
        namedTypeSymbolMock.Setup(s => s.IsGenericType).Returns(true);
        namedTypeSymbolMock.Setup(s => s.OriginalDefinition.SpecialType).Returns(SpecialType.None);

        var result = namedTypeSymbolMock.Object.GetPossiblyNullWrappedType();

        Assert.Equal(namedTypeSymbolMock.Object, result);
    }
}

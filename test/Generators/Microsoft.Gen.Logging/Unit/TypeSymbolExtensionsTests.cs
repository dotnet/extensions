// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.Gen.Logging.Parsing;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.Logging.Test;

public class TypeSymbolExtensionsTests
{
    private readonly Action<DiagnosticDescriptor, Location?, object
        ?[]?> _diagCallback = (_, __, ___) => { };

    [Fact]
    public void ValidateIsEnumerableArray()
    {
        var referencedSource = @"
                namespace Microsoft.Extensions.Logging
                {
                    internal class LoggerMessageAttribute { }
                }
                namespace Microsoft.Extensions.Logging
                {
                    internal interface ILogger { }
                    internal enum LogLevel { }
                }";

        // Compile the referenced assembly first.
        Compilation referencedCompilation = CompilationHelper.CreateCompilation(referencedSource);

        // Obtain the image of the referenced assembly.
        byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

        // Generate the code
        string source = @"
                namespace Test
                {
                    using Microsoft.Extensions.Logging;

                    partial class C
                    {
                        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger);
                    }
                }";

        MetadataReference[] additionalReferences = [MetadataReference.CreateFromImage(referencedImage)];

        Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);
        LoggingGenerator generator = new LoggingGenerator();

        (IReadOnlyList<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources) =
            RoslynTestUtils.RunGenerator(compilation, generator);

        INamedTypeSymbol symbol = compilation.GetSpecialType(SpecialType.System_Array);
        SymbolHolder? symbolHolder = SymbolLoader.LoadSymbols(compilation, _diagCallback);
        Assert.True(symbol.IsEnumerable(symbolHolder));
    }

    [Fact]
    public void ValidateIsEnumerableBoolean()
    {
        var referencedSource = @"
                namespace Microsoft.Extensions.Logging
                {
                    internal class LoggerMessageAttribute { }
                }
                namespace Microsoft.Extensions.Logging
                {
                    internal interface ILogger { }
                    internal enum LogLevel { }
                }";

        // Compile the referenced assembly first.
        Compilation referencedCompilation = CompilationHelper.CreateCompilation(referencedSource);

        // Obtain the image of the referenced assembly.
        byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

        // Generate the code
        string source = @"
                namespace Test
                {
                    using Microsoft.Extensions.Logging;

                    partial class C
                    {
                        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger);
                    }
                }";

        MetadataReference[] additionalReferences = [MetadataReference.CreateFromImage(referencedImage)];

        Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);
        LoggingGenerator generator = new LoggingGenerator();

        (IReadOnlyList<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources) =
            RoslynTestUtils.RunGenerator(compilation, generator);

        INamedTypeSymbol symbol = compilation.GetSpecialType(SpecialType.System_Boolean);
        SymbolHolder? symbolHolder = SymbolLoader.LoadSymbols(compilation, _diagCallback);
        Assert.False(symbol.IsEnumerable(symbolHolder));
    }

    [Fact]
    public void ImplementsIFormattable()
    {
        var referencedSource = @"
                namespace Microsoft.Extensions.Logging
                {
                    internal class LoggerMessageAttribute { }
                }
                namespace Microsoft.Extensions.Logging
                {
                    internal interface ILogger { }
                    internal enum LogLevel { }
                }";

        // Compile the referenced assembly first.
        Compilation referencedCompilation = CompilationHelper.CreateCompilation(referencedSource);

        // Obtain the image of the referenced assembly.
        byte[] referencedImage = CompilationHelper.CreateAssemblyImage(referencedCompilation);

        // Generate the code
        string source = @"
                namespace Test
                {
                    using Microsoft.Extensions.Logging;

                    partial class ShouldFormat : IFormattable
                    {
                        [LoggerMessage(EventId = 1, Level = LogLevel.Debug, Message = ""M1"")]
                        static partial void M1(ILogger logger);

                        public string toString(string? format, IFormatProvider? provider) 
                        {
                            return ""formatted"";
                        }
                    }

                }";

        MetadataReference[] additionalReferences = [MetadataReference.CreateFromImage(referencedImage)];

        Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);
        LoggingGenerator generator = new LoggingGenerator();

        (IReadOnlyList<Diagnostic> diagnostics, ImmutableArray<GeneratedSourceResult> generatedSources) =
            RoslynTestUtils.RunGenerator(compilation, generator);
        var classSymbol = compilation.GetTypeByMetadataName("Test.ShouldFormat");
        SymbolHolder? symbolHolder = SymbolLoader.LoadSymbols(compilation, _diagCallback);
        Assert.True(classSymbol.ImplementsIFormattable(symbolHolder));
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options.Contextual;
using Microsoft.Gen.ContextualOptions.Model;
using Microsoft.Gen.Shared;
using Moq;
using Xunit;

namespace Microsoft.Gen.ContextualOptions.Test;

public class EmitterTests
{
    [Fact]
    public void EmmitterDoesNotMakeStructMethodReadonly()
    {
        var delarations = SyntaxFactory
            .ParseCompilationUnit(File.ReadAllText("TestClasses/Struct1.cs"))
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .ToImmutableArray();
        var type = new OptionsContextType(
            Mock.Of<INamedTypeSymbol>(sym => sym.Name == "Struct1" && sym.ContainingNamespace.ToString() == "Microsoft.GenContextualOptions.TestClasses"),
            delarations,
            ImmutableArray.Create("Foo"));
        var generatedStruct = new Emitter().Emit(new[] { type });
        var syntaxTree = SyntaxFactory.ParseSyntaxTree(generatedStruct);
        Assert.DoesNotContain(
            syntaxTree.GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>().Single().Members.Single().Modifiers,
            mod => mod.IsKind(SyntaxKind.ReadOnlyKeyword));
    }

    [Fact]
    public void EmmitterEmitsReceiveCallForAllProperties()
    {
        var delarations = SyntaxFactory
            .ParseCompilationUnit(File.ReadAllText("TestClasses/Class2A.cs"))
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .Concat(SyntaxFactory
                .ParseCompilationUnit(File.ReadAllText("TestClasses/Class2B.cs"))
                .DescendantNodes()
                .OfType<TypeDeclarationSyntax>())
            .ToImmutableArray();

        var type = new OptionsContextType(
            Mock.Of<INamedTypeSymbol>(sym => sym.Name == "Class2" && sym.ContainingNamespace.ToString() == "Microsoft.GenContextualOptions.TestClasses"),
            delarations,
            ImmutableArray.Create("Foo", "Bar"));
        var generatedStruct = new Emitter().Emit(new[] { type });
        var syntaxTree = SyntaxFactory.ParseSyntaxTree(generatedStruct);
        var statements = syntaxTree!
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single()
            .Body!
            .Statements
            .Select(statement => statement.ToString());

        Assert.Contains("receiver.Receive(nameof(Foo), Foo);", statements);
        Assert.Contains("receiver.Receive(nameof(Bar), Bar);", statements);
    }

    [Fact]
    public void EmmitterEmitsValidRecord()
    {
        var delarations = SyntaxFactory
           .ParseCompilationUnit(File.ReadAllText("TestClasses/Record1.cs"))
           .DescendantNodes()
           .OfType<TypeDeclarationSyntax>()
           .ToImmutableArray();

        var type = new OptionsContextType(
            Mock.Of<INamedTypeSymbol>(sym => sym.Name == "Record1" && sym.ContainingNamespace.ToString() == "Microsoft.GenContextualOptions.TestClasses"),
            delarations,
            ImmutableArray.Create("Foo"));

        var generatedStruct = new Emitter().Emit(new[] { type });
        var syntaxTree = SyntaxFactory.ParseSyntaxTree(generatedStruct);
        var statements = syntaxTree!
            .GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single()
            .Body!
            .Statements
            .Select(statement => statement.ToString());

        Assert.Single(statements, "receiver.Receive(nameof(Foo), Foo);");
    }

    [Fact]
    public void EmmitterEmitsValidNamespacelessType()
    {
        var delarations = SyntaxFactory
           .ParseCompilationUnit(File.ReadAllText("TestClasses/NamespacelessRecord.cs"))
           .DescendantNodes()
           .OfType<TypeDeclarationSyntax>()
           .ToImmutableArray();

        var type = new OptionsContextType(
            Mock.Of<INamedTypeSymbol>(sym => sym.Name == "NamespacelessRecord" && sym.ContainingNamespace.IsGlobalNamespace),
            delarations,
            ImmutableArray.Create("Foo"));

        var generatedStruct = new Emitter().Emit(new[] { type });
        var syntaxTree = SyntaxFactory.ParseSyntaxTree(generatedStruct);
        var hasNamespace = syntaxTree!
            .GetRoot()
            .DescendantNodes()
            .OfType<NamespaceDeclarationSyntax>()
            .Any();

        Assert.False(hasNamespace);
    }

    [Fact]
    public async Task TestEmitter()
    {
        var sources = new List<string>();
        foreach (var file in Directory.GetFiles("TestClasses", "*.cs"))
        {
            sources.Add(File.ReadAllText(file));
        }

        var (d, r) = await RoslynTestUtils.RunGenerator(
            new ContextualOptionsGenerator(),
            new[]
            {
                typeof(OptionsContextAttribute).Assembly,
                typeof(ReadOnlySpan<>).Assembly
            },
            sources)
            .ConfigureAwait(false);

        Assert.Empty(d);
        _ = Assert.Single(r);

        var golden = File.ReadAllText($"GoldenFiles/Microsoft.Gen.ContextualOptions/Microsoft.Gen.ContextualOptions.ContextualOptionsGenerator/ContextualOptions.g.cs");
        var result = r[0].SourceText.ToString();
        Assert.Equal(golden, result);
    }
}

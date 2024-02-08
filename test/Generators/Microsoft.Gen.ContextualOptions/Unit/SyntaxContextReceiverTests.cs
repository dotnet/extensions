// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options.Contextual;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.ContextualOptions.Test;

public class SyntaxContextReceiverTests
{
    [Fact]
    public async Task ShouldCollectTypesWithTheOptionsContextAttribute()
    {
        var sut = new ContextReceiver(CancellationToken.None);
        var sources = new[]
        {
            File.ReadAllText("TestClasses/Class1.cs"),
            File.ReadAllText("TestClasses/Struct1.cs"),
            File.ReadAllText("TestClasses/Record1.cs"),
        };

        var comp = await RoslynTestUtils.RunSyntaxContextReceiver(sut, new[] { typeof(OptionsContextAttribute).Assembly }, sources);

        Assert.True(sut.TryGetTypeDeclarations(comp, out var typeDeclarations));

        Assert.Equal(3, typeDeclarations!.Count);
        foreach (var declaration in typeDeclarations)
        {
            Assert.Equal(declaration.Key.Name, declaration.Value.Single().Identifier.Text);
            Assert.Equal("TestClasses", declaration.Key.ContainingNamespace.ToString());
        }
    }

    [Fact]
    public async Task ShouldDoNothingWithoutTheOptionsContextAttributeReferenced()
    {
        var sut = new ContextReceiver(CancellationToken.None);
        var sources = new[]
        {
            File.ReadAllText("TestClasses/Class1.cs"),
            File.ReadAllText("TestClasses/Struct1.cs"),
            File.ReadAllText("TestClasses/Record1.cs"),
        };

        var comp = await RoslynTestUtils.RunSyntaxContextReceiver(sut, Enumerable.Empty<Assembly>(), sources);

        Assert.False(sut.TryGetTypeDeclarations(comp, out _));
    }

    [Fact]
    public async Task ShouldCollectMultiFileTypesWithTheOptionsContextAttribute()
    {
        var sut = new ContextReceiver(CancellationToken.None);
        var sources = new[]
        {
            File.ReadAllText("TestClasses/Class2A.cs"),
            File.ReadAllText("TestClasses/Class2B.cs"),
        };

        var comp = await RoslynTestUtils.RunSyntaxContextReceiver(sut, new[] { typeof(OptionsContextAttribute).Assembly }, sources);
        Assert.True(sut.TryGetTypeDeclarations(comp, out var typeDeclarations));

        Assert.Single(typeDeclarations!);

        var declaration = typeDeclarations!.Single();
        Assert.Equal("TestClasses", declaration.Key.ContainingNamespace.ToString());
        Assert.Equal(2, declaration.Value.Count);
        Assert.All(declaration.Value.Select(dec => dec.Identifier.Text), className => Assert.Equal("Class2", className));
        Assert.NotEqual(declaration.Value[0], declaration.Value[1]);
    }

    [Fact]
    public async Task ShouldIgnoreTypesWithoutTheOptionsContextAttribute()
    {
        var sut = new ContextReceiver(CancellationToken.None);
        var sources = new[] { File.ReadAllText("TestClasses/ClassWithNoAttribute.cs"), };

        var comp = await RoslynTestUtils.RunSyntaxContextReceiver(sut, new[] { typeof(OptionsContextAttribute).Assembly }, sources);

        Assert.True(sut.TryGetTypeDeclarations(comp, out var typeDeclarations));
        Assert.Empty(typeDeclarations!);
    }
}

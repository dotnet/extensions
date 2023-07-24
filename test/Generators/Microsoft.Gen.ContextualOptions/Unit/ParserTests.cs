// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options.Contextual;
using Microsoft.Gen.ContextualOptions.Model;
using Microsoft.Gen.Shared;
using Xunit;

namespace Microsoft.Gen.ContextualOptions.Test;

public class ParserTests
{
    [Fact]
    public async Task ShouldWarnWithUnusableProperties()
    {
        var sources = new[]
        {
            File.ReadAllText("TestClasses/ClassWithUnusableProperties.txt"),
        };

        var result = await GetParserResult(sources);

        Assert.Single(result);
        Assert.Empty(result!.Single().OptionsContextProperties);
        Assert.Equal(DiagDescriptors.ContextDoesNotHaveValidProperties, result!.Single().Diagnostics.Single().Descriptor);
        Assert.True(result.Single().ShouldEmit);
        Assert.Equal("TestClasses.ClassWithUnusableProperties", result.Single().HintName);
    }

    [Fact]
    public async Task ShouldReturnValidProperties()
    {
        var sources = new[]
        {
            File.ReadAllText("TestClasses/NamespacelessRecord.cs"),
        };

        var result = await GetParserResult(sources);

        Assert.Single(result);
        Assert.Empty(result!.Single().Diagnostics);
        Assert.Equal("Foo", result!.Single().OptionsContextProperties.Single());
        Assert.True(result.Single().ShouldEmit);
        Assert.Equal(".NamespacelessRecord", result.Single().HintName);
    }

    [Fact]
    public async Task ShouldErrorWhenAttributeAppliedToNonPartialClass()
    {
        var sources = new[]
        {
            File.ReadAllText("TestClasses/NonPartialClass.txt"),
        };

        var result = await GetParserResult(sources);

        Assert.Single(result);
        Assert.Equal(DiagDescriptors.ContextMustBePartial, result!.Single().Diagnostics.Single().Descriptor);
        Assert.False(result.Single().ShouldEmit);
    }

    [Fact]
    public async Task ShouldErrorWhenAttributeAppliedToStaticClass()
    {
        var sources = new[]
        {
            File.ReadAllText("TestClasses/StaticClass.txt"),
        };

        var result = await GetParserResult(sources);

        Assert.Single(result);
        Assert.Contains(result!.Single().Diagnostics, diag => diag.Descriptor == DiagDescriptors.ContextCannotBeStatic);
        Assert.False(result.Single().ShouldEmit);
    }

    [Fact]
    public async Task ShouldErrorWhenAttributeAppliedToRefStruct()
    {
        var sources = new[]
        {
            File.ReadAllText("TestClasses/RefStruct.txt"),
        };

        var result = await GetParserResult(sources);

        Assert.Single(result);
        Assert.Contains(result!.Single().Diagnostics, diag => diag.Descriptor == DiagDescriptors.ContextCannotBeRefLike);
        Assert.False(result.Single().ShouldEmit);
    }

    private static async Task<IEnumerable<OptionsContextType>> GetParserResult(string[] sources) =>
        (await RoslynTestUtils.RunParser(
            new ContextReceiver(CancellationToken.None),
            (receiver, compilation) =>
            {
                Assert.True(receiver.TryGetTypeDeclarations(compilation, out var typeDeclarations));
                return Parser.GetContextualOptionTypes(typeDeclarations!);
            },
            new[] { typeof(OptionsContextAttribute).Assembly, typeof(ReadOnlySpan<>).Assembly },
            sources).ConfigureAwait(true))!;
}

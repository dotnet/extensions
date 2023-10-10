// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public static class CoalesceAnalyzerTests
{
    [Fact]
    public static async Task CoalesceAssignmentWithTask()
    {
        const string Source = @"
            using System;
            using System.Threading.Tasks;

            namespace Example
            {
                public class Foo
                {
                }

                public class Test
                {
                    public async Task<DateTime?> GetDate()
                    {
                        return await GetSomething() ?? await GetSomethingElse();
                    }

                    private Task<DateTime?> GetSomething() { return Task.FromResult((DateTime?)DateTime.Now); }
                    private Task<DateTime?> GetSomethingElse() { return Task.FromResult((DateTime?)DateTime.Now); }
                }
            }
        ";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CoalesceAnalyzer(),
            null,
            new[] { Source });

#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.
        Assert.Equal(0, d.Count);
#pragma warning restore xUnit2013 // Do not use equality check to check for collection size.
    }

    [Fact]
    public static async Task CoalesceAssignment()
    {
        const string Source = @"
            using System.Diagnostics.CodeAnalysis;

            namespace Example
            {
                public class Foo
                {
                }

                public class Test
                {
                    public Foo? NullableFooProp { get; set; }
                    public Foo FooProp { get; set; } = new Foo();
                    [MaybeNull]
                    public Foo FooPropMaybeNull { get; set; } = new Foo();

                    public Foo? NullableFooField;
                    public Foo FooField = new Foo();
                    [MaybeNull]
                    public Foo FooFieldMaybeNull = new Foo();

                    public void Method()
                    {
                        string s = ""Hello"";
                        /*0+*/s ??= ""World""/*-0*/;

                        NullableFooProp ??= new Foo();
                        /*1+*/FooProp ??= new Foo()/*-1*/;
                        FooPropMaybeNull ??= new Foo();

                        NullableFooField ??= new Foo();
                        /*2+*/FooField ??= new Foo()/*-2*/;
                        FooFieldMaybeNull ??= new Foo();

                        Foo? nullableFooLocal = null;
                        Foo fooLocal = new Foo();

                        nullableFooLocal ??= new Foo();
                        /*3+*/fooLocal ??= new Foo()/*-3*/;
                    }
                }
            }
        ";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CoalesceAnalyzer(),
            null,
            new[] { Source });

        Assert.Equal(4, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.CoalesceAssignment, d[i]);
        }
    }

    [Fact]
    public static async Task Coalesce()
    {
        const string Source = @"
            using System.Diagnostics.CodeAnalysis;

            namespace Example
            {
                public class Foo
                {
                }

                public class Test
                {
                    public Foo? NullableFooProp { get; set; }
                    public Foo FooProp { get; set; } = new Foo();
                    [MaybeNull]
                    public Foo FooPropMaybeNull { get; set; } = new Foo();

                    public Foo? NullableFooField;
                    public Foo FooField = new Foo();
                    [MaybeNull]
                    public Foo FooFieldMaybeNull = new Foo();

                    public Foo? NullableFooMethod() => new Foo();
                    public Foo FooMethod() => new Foo();
                    [return: MaybeNull]
                    public Foo FooMethodMaybeNull() => new Foo();

                    public void Method()
                    {
                        string s = ""Hello"";
                        _ = /*0+*/s ?? ""World""/*-0*/;

                        _ = NullableFooProp ?? new Foo();
                        _ = /*1+*/FooProp ?? new Foo()/*-1*/;
                        _ = FooPropMaybeNull ?? new Foo();

                        _ = NullableFooField ?? new Foo();
                        _ = /*2+*/FooField ?? new Foo()/*-2*/;
                        _ = FooFieldMaybeNull ?? new Foo();

                        _ = NullableFooMethod() ?? new Foo();
                        _ = /*3+*/FooMethod() ?? new Foo()/*-3*/;
                        _ = FooMethodMaybeNull() ?? new Foo();

                        Foo? nullableFooLocal = null;
                        Foo fooLocal = new Foo();

                        _ = nullableFooLocal ?? new Foo();
                        _ = /*4+*/fooLocal ?? new Foo()/*-4*/;
                    }
                }
            }
        ";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CoalesceAnalyzer(),
            null,
            new[] { Source });

        Assert.Equal(5, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.Coalesce, d[i]);
        }
    }
}

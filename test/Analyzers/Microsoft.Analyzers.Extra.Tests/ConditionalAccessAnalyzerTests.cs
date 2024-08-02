// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public static class ConditionalAccessAnalyzerTests
{
    [Fact]
    public static async Task Basic()
    {
        const string Source = @"
            using System.Diagnostics.CodeAnalysis;

            namespace Example
            {
                public class Foo
                {
                    public void Invoke() { }
                    public int Prop { get; }
                    public int Field;
                }

                public class Test
                {
                    public Foo? GetNullableFoo() { return null; }
                    public Foo GetFoo() { return null!; }
                    [return: MaybeNull]
                    public Foo GetFooMaybeNull() { return null!; }

                    public Foo? NullableFoo { get; set; }
                    public Foo Foo { get; set; } = new Foo();
                    [MaybeNull]
                    public Foo FooMaybeNull { get; set; } = new Foo();

                    public void PublicMethod(string s1, string? s2)
                    {
                        var l0 = s1?.Length;
                        var l1 = s2?.Length;
                    }

                    internal void InternalMethod(string s1, string? s2)
                    {
                        var l0 = /*0+*/(s1.Substring(0) + ""a"")?.Length/*-0*/;
                        var l1 = s2?.Length;
                    }

                    public void Method()
                    {
                        GetNullableFoo()?.Invoke();
                        /*1+*/GetFoo()?.Invoke()/*-1*/;
                        GetFooMaybeNull()?.Invoke();

                        _ = GetNullableFoo()?.Prop;
                        _ = /*2+*/GetFoo()?.Prop/*-2*/;
                        _ = GetFooMaybeNull()?.Prop;

                        _ = GetNullableFoo()?.Field;
                        _ = /*3+*/GetFoo()?.Field/*-3*/;
                        _ = GetFooMaybeNull()?.Field;

                        NullableFoo?.Invoke();
                        /*4+*/Foo?.Invoke()/*-4*/;
                        FooMaybeNull?.Invoke();

                        _ = NullableFoo?.Prop;
                        _ = /*5+*/Foo?.Prop/*-5*/;
                        _ = FooMaybeNull?.Prop;

                        _ = NullableFoo?.Field;
                        _ = /*6+*/Foo?.Field/*-6*/;
                        _ = FooMaybeNull?.Field;
                    }

                    public void Method2(int? p)
                    {
                        int? nullable = p;

                        _ = nullable?.ToString();
                        if (nullable != null)
                        {
                            _ = /*7+*/nullable?.ToString()/*-7*/;
                        }
                    }

                    internal readonly struct Arg<TParameter>
                    {
                        private readonly int _hash;

                        public Arg(TParameter arg1)
                        {
                            Arg1 = arg1;
                            _hash = Arg1?.GetHashCode() ?? 0;
                        }

                        public readonly TParameter Arg1;
                    }
                }
            }
        ";

        var d = await RoslynTestUtils.RunAnalyzer(
            new ConditionalAccessAnalyzer(),
            null,
            new[] { Source });

#if NET6_0_OR_GREATER
        Assert.Equal(8, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.ConditionalAccess, d[i]);
        }
#else
        Assert.Equal(0, d.Count);
#endif
    }
}

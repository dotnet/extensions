// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.Test;

public static class OptimizeArraysAnalyzerTests
{
    [Fact]
    public static async Task MakeArrayStatic()
    {
        const string Source = """
            using System.Collections.Generic;
            using System.Collections.Immutable;

            namespace Example
            {
                public enum Color { Red, Green, Blue }

                public class Test
                {
                    private readonly int[] _nums = /*0+*/new[] { 1, 2, 3 }/*-0*/;
                    private readonly Color[] _colors = /*1+*/new[] { Color.Red, Color.Green, Color.Blue }/*-1*/;
                    private readonly string[] _strings = /*2+*/new[] { "One", "Two", "Three" }/*-2*/;

                    public void Method(string[] stuff) { }

                    public void Method2()
                    {
                        var a = /*3+*/new[] { 1, 2, 3 }/*-3*/;
                        Method(/*4+*/new[] { "One", "Two", "Three" }/*-4*/);
                    }

                    public string[] Prop { get; } = /*5+*/new[] { "One", "Two", "Three" }/*-5*/;

                    public static int HowMany(string[] strings)
                    {
                        return strings.Length;
                    }

                    private static readonly int[] _staticNums = new[] { 1, 2, 3 };
                    private static readonly int _numStrings = HowMany(new[] { "One", "Two", "Three" });
                    public static int NumStrings { get; } = HowMany(new[] { "One", "Two", "Three" });
                    public static ImmutableArray<string> Immutable { get; } = ImmutableArray.Create(new[] { "One", "Two", "Three" });
                    public static IEnumerable<string> All { get; } = new[] { "One", "Two", "Three" };
                }
            }
        """;

        var d = await RoslynTestUtils.RunAnalyzer(
            new OptimizeArraysAnalyzer(),
            new[] { Assembly.GetAssembly(typeof(ImmutableArray))! },
            new[] { Source }).ConfigureAwait(false);

        Assert.Equal(6, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.MakeArrayStatic, d[i]);
        }
    }
}

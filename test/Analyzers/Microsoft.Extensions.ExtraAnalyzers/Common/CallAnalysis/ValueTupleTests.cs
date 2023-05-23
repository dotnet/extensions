// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.Extensions.ExtraAnalyzers.Test;
using Xunit;

namespace Microsoft.Extensions.ExtraAnalyzers.CallAnalysis.Test;

public static class ValueTupleTests
{
    [Fact]
    public static async Task ValueTuple()
    {
        const string Source = @"
            using System;

            namespace Example
            {
                public class Test
                {
                    public void Foo()
                    {
                        _ = /*0+*/new Tuple<int>(0)/*-0*/;
                        _ = /*1+*/new Tuple<int, int>(0, 1)/*-1*/;
                        _ = /*2+*/new Tuple<int, int, int>(0, 1, 2)/*-2*/;
                        _ = /*3+*/new Tuple<int, int, int, int>(0, 1, 2, 3)/*-3*/;
                        _ = /*4+*/new Tuple<int, int, int, int, int>(0, 1, 2, 3, 4)/*-4*/;
                        _ = /*5+*/new Tuple<int, int, int, int, int, int>(0, 1, 2, 3, 4, 5)/*-5*/;
                        _ = /*6+*/new Tuple<int, int, int, int, int, int, int>(0, 1, 2, 3, 4, 5, 6)/*-6*/;
                        _ = /*7+*/new Tuple<int, int, int, int, int, int, int, int>(0, 1, 2, 3, 4, 5, 6, 7)/*-7*/;

                        _ = /*8+*/Tuple.Create(0)/*-8*/;
                        _ = /*9+*/Tuple.Create(0, 1)/*-9*/;
                        _ = /*10+*/Tuple.Create(0, 1, 2)/*-10*/;
                        _ = /*11+*/Tuple.Create(0, 1, 2, 3)/*-11*/;
                        _ = /*12+*/Tuple.Create(0, 1, 2, 3, 4)/*-12*/;
                        _ = /*13+*/Tuple.Create(0, 1, 2, 3, 4, 5)/*-13*/;
                        _ = /*14+*/Tuple.Create(0, 1, 2, 3, 4, 5, 6)/*-14*/;
                        _ = /*15+*/Tuple.Create(0, 1, 2, 3, 4, 5, 6, 7)/*-15*/;
                    }
                }
            }
        ";

        var d = await RoslynTestUtils.RunAnalyzer(
            new CallAnalyzer(),
            null,
            new[] { Source }).ConfigureAwait(false);

        Assert.Equal(16, d.Count);
        for (int i = 0; i < d.Count; i++)
        {
            Source.AssertDiagnostic(i, DiagDescriptors.ValueTuple, d[i]);
        }
    }
}
